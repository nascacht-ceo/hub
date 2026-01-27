using nc.Google;
using System.Net;
using System.Text;
using System.Text.Json;

namespace nc.Storage;

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
	public CredentialCache CredentialCache { get; init; } = [];

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
		{ "file", StorageProviders.Disk },
		{ "c", StorageProviders.Disk },
		{ "d", StorageProviders.Disk }

	};

	/// <summary>
	/// Gets the default host URI used for storage operations.
	/// </summary>
	public string DefaultHost { get; init; } = $"{StorageProviders.Disk}://./";

	/// <summary>
	/// When true, parallel operations should throw internal errors.
	/// Defaults to true.
	/// </summary>
	public bool ThrowTPLErrors { get; set; } = true;

	/// <summary>
	/// Gets the default AWS region to use for service operations.
	/// </summary>
	/// <remarks>This value determines the AWS region that is used when no region is explicitly specified for an
	/// operation. The default is "us-east-1".</remarks>
	public string AwsRegionDefault { get; init; } = "us-east-1";

	/// <summary>
	/// Gets the collection of Google Cloud Platform (GCP) service account identities associated with this instance.
	/// </summary>
	/// <remarks>The dictionary maps identity names to their corresponding <see cref="GoogleTenant"/> objects.
	/// Key comparison is case-insensitive using ordinal rules.</remarks>
	public IDictionary<string, GoogleTenant> GcpIdentities { get; init; }
		= new Dictionary<string, GoogleTenant>(StringComparer.OrdinalIgnoreCase);
	
	/// <summary>
	/// Retrieves the Google Cloud Platform (GCP) credential associated with the specified key, encoded as a Base64 JSON
	/// string.
	/// </summary>
	/// <param name="key">The key that identifies the GCP credential to retrieve. Must correspond to an existing entry in the credential
	/// store.</param>
	/// <returns>A Base64-encoded string containing the JSON representation of the GCP credential associated with the specified key.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if the specified key does not exist in the GCP credential store.</exception>
	public string GetGcpCredential(string key)
	{
		if (!GcpIdentities.TryGetValue(key, out GoogleTenant? tenant))
			throw new ArgumentOutOfRangeException(nameof(key), $"No GCP credentials exist for this key. Valid values are: {string.Join(",", GcpIdentities.Keys)}");
		var jsonString = tenant.ToServiceAccountJson();
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonString));
	}
}