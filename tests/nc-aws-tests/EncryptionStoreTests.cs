using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using nc.Cloud.Models.Encryption;

namespace nc.Aws.Tests;

/// <summary>
/// Integration tests for the EncryptionStore against a running LocalStack environment.
/// Requires a running LocalStack container with the Secrets Manager service enabled.
/// </summary>
public class EncryptionStoreTests : IDisposable
{
	private readonly EncryptionStore _store;
	private readonly EncryptionStoreOptions _options;
	private readonly IAmazonSecretsManager _secretsManagerClient;
	private readonly List<string> _keysToCleanup = new List<string>();

	public EncryptionStoreTests()
	{
		// 1. Setup Options for LocalStack
		_options = new EncryptionStoreOptions();

		var config = new ConfigurationBuilder()
			.AddUserSecrets("nc-hub")
			.Build()
			.GetSection("tests:aws");

		// 2. Configure AWS Client for LocalStack
		// The ServiceURL points to LocalStack. 
		// Credentials are dummy since LocalStack accepts any valid credentials.
		var clientConfig = new AmazonSecretsManagerConfig
		{
			ServiceURL = config["localstackurl"],
			AuthenticationRegion = "us-east-1" // Must use a region that LocalStack recognizes
		};

		_secretsManagerClient = new AmazonSecretsManagerClient(
			new Amazon.Runtime.BasicAWSCredentials("dummy", "dummy"),
			clientConfig
		);

		// 3. Setup Logger (using NullLogger for testing) and Options for IOptions pattern
		var logger = new NullLogger<EncryptionStore>();
		var optionsMock = Options.Create(_options);

		// 4. Instantiate the Service Under Test
		_store = new EncryptionStore(optionsMock, _secretsManagerClient, logger);
	}

	/// <summary>
	/// Tests the complete lifecycle: Set (Create/Update) and Get.
	/// </summary>
	[Fact]
	public async Task SetKeyPairAsync_And_GetKeyPairAsync_Should_Succeed()
	{
		// ARRANGE
		string testId = $"broker-{Guid.NewGuid():N}";
		var originalKeyPair = KeyPair.Create(testId);
		_keysToCleanup.Add(testId);

		// ACT 1: Create
		await _store.SetKeyPairAsync(testId, originalKeyPair);

		// ACT 2: Retrieve
		var retrievedKeyPair = await _store.GetKeyPairAsync(testId);

		// ASSERT 1: Verify retrieval and content integrity
		Assert.NotNull(retrievedKeyPair);
		Assert.Equal(testId, retrievedKeyPair!.Id);
		Assert.Equal(originalKeyPair.PrivateKey, retrievedKeyPair.PrivateKey);
		Assert.Equal(originalKeyPair.PublicKey, retrievedKeyPair.PublicKey);

		// ARRANGE 2: Update the key data for a subsequent Set
		var updatedPrivateKey = "---BEGIN NEW PRIVATE KEY---" + Guid.NewGuid().ToString() + "---END NEW PRIVATE KEY---";
		var updatedKeyPair = new nc.Cloud.Models.Encryption.KeyPair
		{
			Id = testId,
			PrivateKey = updatedPrivateKey,
			PublicKey = originalKeyPair.PublicKey // Keep public key the same
		};

		// ACT 3: Update
		await _store.SetKeyPairAsync(testId, updatedKeyPair);

		// ACT 4: Retrieve Updated
		var updatedRetrievedKeyPair = await _store.GetKeyPairAsync(testId);

		// ASSERT 2: Verify the key was updated
		Assert.NotNull(updatedRetrievedKeyPair);
		Assert.Equal(updatedPrivateKey, updatedRetrievedKeyPair!.PrivateKey);
	}

	/// <summary>
	/// Tests the Delete operation and verifies the secret is scheduled for deletion in AWS.
	/// </summary>
	[Fact]
	public async Task DeleteKeyPairAsync_Should_ScheduleDeletion()
	{
		// ARRANGE
		string testId = $"broker-delete-{Guid.NewGuid():N}";
		var keyPair = KeyPair.Create(testId);

		// Ensure the secret exists before attempting to delete it
		await _store.SetKeyPairAsync(testId, keyPair);

		// ACT: Delete the key pair
		await _store.DeleteKeyPairAsync(testId);

		// ASSERT 1: The key should no longer be retrievable via GetKeyPairAsync
		var retrievedAfterDelete = await _store.GetKeyPairAsync(testId);
		Assert.Null(retrievedAfterDelete);

		// ASSERT 2: Verify deletion metadata in Secrets Manager (using AWS SDK directly)
		var secretId = _options.KeyPrefix + testId;
		var describeRequest = new DescribeSecretRequest { SecretId = secretId };

		try
		{
			var describeResponse = await _secretsManagerClient.DescribeSecretAsync(describeRequest);

			// Should be scheduled for deletion, but the object still exists until the window passes
			Assert.NotNull(describeResponse.DeletedDate);

			// Verify the recovery window used matches the configured option
			var expectedDeletionTime = DateTime.UtcNow.AddDays(_options.RecoveryWindowInDays);
			var actualDeletionTime = describeResponse.DeletedDate.Value;

			// Check if the deletion date is within a few minutes of the expected date
			Assert.True(actualDeletionTime > DateTime.UtcNow);
			Assert.True((actualDeletionTime - expectedDeletionTime).TotalMinutes < 5);
		}
		catch (Exception ex)
		{
			Assert.Fail($"Failed to describe secret after deletion: {ex.Message}");
		}
	}

	/// <summary>
	/// Cleans up any resources created during testing by forcing immediate deletion 
	/// (LocalStack is usually configured to allow this).
	/// </summary>
	public void Dispose()
	{
		_secretsManagerClient.Dispose();
	}
}
