using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace nc.Extensions.FluentStorage;

/// <summary>
/// Represents configuration options for connecting to and authenticating with various storage services, including AWS
/// S3, Azure Blob, Google Cloud Storage, and others.
/// </summary>
/// <remarks>This record provides settings for authentication, credential management, and service prefix mapping
/// to support multiple storage backends in a unified manner. It is typically used to configure storage service clients
/// or factories that require information about authentication types, credentials, and service-specific
/// options.</remarks>
public record StorageServiceOptions
{
	/// <summary>
	/// Gets the authentication type used by the provider.
	/// Default is "FluentStorage".
	/// </summary>
	public string AuthenticationType { get; init; }
		= "FluentStorage";

	/// <summary>
	/// Gets the number of items to process in each batch operation.
	/// </summary>
	/// <remarks>A larger batch size may improve throughput but can increase memory usage. The value is set during
	/// object initialization and cannot be changed afterward.</remarks>
	public int BatchSize { get; init; } = 1000;


	/// <summary>
	/// Gets the collection of network credentials associated with authentication schemes and hosts.
	/// </summary>
	public CredentialCache CredentialCache { get; init; }
		= new CredentialCache();

	/// <summary>
	/// Gets the mapping of storage provider prefixes to their canonical provider names.
	/// </summary>
	/// <remarks>The mapping is case-insensitive and can be used to resolve various common or legacy provider
	/// prefixes to a standard provider identifier. This is useful for normalizing user input or configuration values that
	/// may use different naming conventions for the same storage provider.</remarks>
	public IDictionary<string, string> PrefixMapping { get; init; }
		= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		{ StorageProviders.AwsS3, StorageProviders.AwsS3 },
		{ "aws", StorageProviders.AwsS3 },
		{ "s3", StorageProviders.AwsS3 },

		{ StorageProviders.AzureBlob, StorageProviders.AzureBlob },
		{ StorageProviders.AzureFile, StorageProviders.AzureFile },
		{ "azure", StorageProviders.AzureBlob },

		{ StorageProviders.GoogleStorage, StorageProviders.GoogleStorage },
		{ "gcp", StorageProviders.GoogleStorage},
		{ "google", StorageProviders.GoogleStorage },
		{ "google.gcp", StorageProviders.GoogleStorage },

		{ StorageProviders.Ftp, StorageProviders.Ftp },
		{ StorageProviders.Sftp, StorageProviders.Sftp },
		{ StorageProviders.Memory, StorageProviders.Memory },
		
		{ StorageProviders.Disk, StorageProviders.Disk },
		{ "file", StorageProviders.Disk }

	};

	public string AwsRegionDefault { get; init; } = "us-east-1";

	public IDictionary<string, GcpServiceAccount> GcpIdentities { get; init; }
		= new Dictionary<string, GcpServiceAccount>(StringComparer.OrdinalIgnoreCase);
	

	public string GetGcpCredential(string key)
	{
		if (!GcpIdentities.ContainsKey(key))
			throw new ArgumentOutOfRangeException("key", $"No GCP credentials exist for this key. Valid values are: {string.Join(",", GcpIdentities.Keys)}");
		var jsonString = JsonSerializer.Serialize(GcpIdentities[key]);
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonString));
	}
}

public record GcpServiceAccount
{
	[JsonPropertyName("type")]
	public string Type { get; init; } = "service_account";

	[JsonPropertyName("project_id")]
	public required string ProjectId { get; init; }

	[JsonPropertyName("private_key_id")]
	public string? PrivateKeyId { get; init; }

	[JsonPropertyName("private_key")]
	public required string PrivateKey { get; init; }

	[JsonPropertyName("client_email")]
	public required string ClientEmail { get; init; }

	[JsonPropertyName("client_id")]
	public string? ClientId { get; init; }

	[JsonPropertyName("auth_uri")]
	public string AuthUri { get; init; } = "https://accounts.google.com/o/oauth2/auth";

	[JsonPropertyName("token_uri")]
	public string TokenUri { get; init; } = "https://oauth2.googleapis.com/token";

	[JsonPropertyName("auth_provider_x509_cert_url")]
	public string AuthProviderCertUrl { get; init; } = "https://www.googleapis.com/oauth2/v1/certs";

	[JsonPropertyName("client_x509_cert_url")]
	public string? ClientCertUrl { get; init; }
}