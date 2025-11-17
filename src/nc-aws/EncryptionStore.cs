using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nc.Cloud;
using nc.Cloud.Models.Encryption;
using System.Security.Cryptography;
using System.Text.Json;

namespace nc.Aws;

/// <summary>
/// AWS-specific implementation of IEncryptionStore using AWS Secrets Manager.
/// Assumes the application is running on an AWS resource with an IAM Role 
/// that has necessary permissions (GetSecretValue, PutSecretValue, DeleteSecret).
/// </summary>
public class EncryptionStore : IEncryptionStore
{
	private readonly EncryptionStoreOptions _options;
	private readonly IAmazonSecretsManager _secretsManagerClient;
	private readonly ILogger<EncryptionStore>? _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="EncryptionStore"/> class with the specified options, AWS Secrets
	/// Manager client, and optional logger?.
	/// </summary>
	/// <param name="options">The configuration options for the encryption store. This parameter cannot be null.</param>
	/// <param name="secretsManagerClient">The AWS Secrets Manager client used to retrieve and manage secrets. This parameter cannot be null.</param>
	/// <param name="logger">An optional logger instance for logging diagnostic messages. If null, no logging will be performed.</param>
	public EncryptionStore(IOptions<EncryptionStoreOptions> options, IAmazonSecretsManager secretsManagerClient, ILogger<EncryptionStore>? logger = null)
	{
		_options = options.Value;
		_secretsManagerClient = secretsManagerClient;
		_logger = logger;
	}

	private string GetSecretId(string id) => $"{_options.KeyPrefix}{id}";

	/// <summary>
	/// Retrieves the public and private key material associated with a specific ID.
	/// </summary>
	public async Task<KeyPair?> GetKeyPairAsync(string id, CancellationToken cancellationToken = default)
	{
		var secretId = GetSecretId(id);
		_logger?.LogInformation("Attempting to retrieve key data for ID: {Id}", id);

		var request = new GetSecretValueRequest { SecretId = secretId };

		try
		{
			var response = await _secretsManagerClient.GetSecretValueAsync(request);

			if (string.IsNullOrEmpty(response.SecretString))
			{
				_logger?.LogWarning("Secret {SecretId} found but contained no SecretString value.", secretId);
				return null;
			}

			// Deserialize the JSON payload into our EncryptionKeyPair model
			var keyData = JsonSerializer.Deserialize<KeyPair>(response.SecretString,
				new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			if (keyData == null || string.IsNullOrEmpty(keyData.PrivateKey))
			{
				_logger?.LogError("Secret {SecretId} content was invalid or missing private key.", secretId);
				throw new CryptographicException($"Key data for ID {id} found but was structurally invalid.");
			}

			_logger?.LogInformation("Successfully retrieved key data for ID: {Id}", id);
			keyData.Id = id; // Ensure the ID in the model matches the requested ID
			return keyData;
		}
		catch (ResourceNotFoundException)
		{
			_logger?.LogWarning("Secret for keypair {Id} not found at {SecretId}.", id, secretId);
			return null;
		}
		catch (InvalidRequestException ex)
		{
			_logger?.LogError(ex, "Invalid request when accessing secret {SecretId}.", secretId);
			if (ex.Message.Contains("marked for deletion"))
				return null;
			throw;
		}
		catch (AmazonSecretsManagerException ex)
		{
			_logger?.LogError(ex, "AWS Secrets Manager error while getting key for {SecretId}.", secretId);
			throw;
		}
		catch (JsonException ex)
		{
			_logger?.LogError(ex, "Failed to parse JSON content from secret {SecretId}.", secretId);
			throw new InvalidDataException($"The content of secret {secretId} is not valid JSON.", ex);
		}
	}

	/// <summary>
	/// Saves or updates the public and private key material associated with a specific ID.
	/// </summary>
	public async Task SetKeyPairAsync(string id, KeyPair keyData, CancellationToken cancellationToken = default)
	{
		var secretId = GetSecretId(id);
		_logger?.LogInformation("Attempting to save key data for ID: {Id}", id);

		// Serialize the key data model to a secure JSON string
		var secretJson = JsonSerializer.Serialize(keyData);

		var request = new PutSecretValueRequest
		{
			SecretId = secretId,
			SecretString = secretJson
		};

		try
		{
			var response = await _secretsManagerClient.PutSecretValueAsync(request);
			_logger?.LogInformation("Successfully saved key data for ID {Id}. Version: {VersionId}",
				id, response.VersionId);
		}
		catch (ResourceNotFoundException)
		{
			// If PutSecretValue fails because the secret doesn't exist, we must use CreateSecret
			_logger?.LogInformation("Secret {SecretId} not found. Attempting to create secret.", secretId);
			await CreateSecretInternalAsync(secretId, secretJson);
		}
		catch (AmazonSecretsManagerException ex)
		{
			_logger?.LogError(ex, "AWS Secrets Manager error while setting key for {SecretId}.", secretId);
			throw;
		}
	}

	/// <summary>
	/// Schedules the deletion of the public and private key material associated with a specific ID.
	/// </summary>
	public async Task DeleteKeyPairAsync(string id, CancellationToken cancellationToken = default)
	{
		var secretId = GetSecretId(id);
		_logger?.LogWarning("Scheduling deletion for keypair: {Id}. Will be permanently deleted after {Days} days.",
			id, _options.RecoveryWindowInDays);

		var request = new DeleteSecretRequest
		{
			SecretId = secretId,
			// Best practice: Use a recovery window to prevent immediate, irreversible data loss.
			RecoveryWindowInDays = _options.RecoveryWindowInDays
		};

		try
		{
			var response = await _secretsManagerClient.DeleteSecretAsync(request);
			_logger?.LogInformation("Secret {SecretId} scheduled for deletion. Deletion Date: {DeletionDate}",
				secretId, response.DeletionDate);
		}
		catch (ResourceNotFoundException)
		{
			_logger?.LogWarning("Attempted to delete non-existent secret for keypair: {Id}", id);
			// No need to throw, the desired state (deletion) is achieved.
		}
		catch (AmazonSecretsManagerException ex)
		{
			_logger?.LogError(ex, "AWS Secrets Manager error while deleting key for {SecretId}.", secretId);
			throw;
		}
	}

	/// <summary>
	/// Internal method to handle initial secret creation (used by SetKeyPairAsync).
	/// </summary>
	private async Task CreateSecretInternalAsync(string secretId, string secretJson)
	{
		var createRequest = new CreateSecretRequest
		{
			Name = secretId,
			SecretString = secretJson,
			Description = $"PKI key pair for ID {secretId.Replace(_options.KeyPrefix, "")}"
		};

		try
		{
			var createResponse = await _secretsManagerClient.CreateSecretAsync(createRequest);
			_logger?.LogInformation("Successfully created new secret {SecretId}. ARN: {ARN}", secretId, createResponse.ARN);
		}
		catch (AmazonSecretsManagerException ex)
		{
			// Catch race conditions where another thread/process created it simultaneously
			_logger?.LogError(ex, "Failed to create secret {SecretId}.", secretId);
			throw;
		}
	}
}
