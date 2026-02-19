using Google.Apis.Auth.OAuth2;
using nc.Cloud;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace nc.Google;

/// <summary>
/// Represents configuration settings for a Google Cloud Platform (GCP) tenant, including credentials and project details.
/// </summary>
/// <remarks>This class encapsulates the information required to connect to GCP services on behalf of a specific tenant.
/// It supports conversion to a GoogleCredential instance for use with GCP SDK clients.
/// Credentials can be provided in multiple ways (in order of precedence):
/// <list type="number">
/// <item>Direct credentials: ClientEmail + PrivateKey</item>
/// <item>JSON content: CredentialsJson</item>
/// <item>JSON file: CredentialsPath</item>
/// <item>Application Default Credentials (ADC) if none of the above are provided</item>
/// </list>
/// The class can also be deserialized directly from a GCP service account JSON file.</remarks>
public class GoogleTenant : ITenant
{
	#region ITenant
	/// <summary>
	/// Gets or sets the unique identifier of the tenant associated with the current context.
	/// </summary>
	public required string TenantId { get; set; }

	/// <summary>
	/// Gets or sets the unique name associated with the entity.
	/// </summary>
	[Key]
	public required string Name { get; set; }
	#endregion

	#region Service Account Credentials (mirrors GCP service account JSON)
	/// <summary>
	/// Gets the type of the account. Default is "service_account".
	/// </summary>
	[JsonPropertyName("type")]
	public string Type { get; set; } = "service_account";

	/// <summary>
	/// Gets or sets the GCP project ID.
	/// </summary>
	[JsonPropertyName("project_id")]
	public string? ProjectId { get; set; }

	/// <summary>
	/// Gets or sets the identifier of the private key associated with the credentials.
	/// </summary>
	[JsonPropertyName("private_key_id")]
	public string? PrivateKeyId { get; set; }

	/// <summary>
	/// Gets or sets the private key in PEM format.
	/// </summary>
	[JsonPropertyName("private_key")]
	public string? PrivateKey { get; set; }

	/// <summary>
	/// Gets or sets the service account email address.
	/// </summary>
	[JsonPropertyName("client_email")]
	public string? ClientEmail { get; set; }

	/// <summary>
	/// Gets or sets the client identifier.
	/// </summary>
	[JsonPropertyName("client_id")]
	public string? ClientId { get; set; }

	/// <summary>
	/// Gets or sets the URI used for authentication.
	/// </summary>
	[JsonPropertyName("auth_uri")]
	public string AuthUri { get; set; } = "https://accounts.google.com/o/oauth2/auth";

	/// <summary>
	/// Gets or sets the URI used for token exchange.
	/// </summary>
	[JsonPropertyName("token_uri")]
	public string TokenUri { get; set; } = "https://oauth2.googleapis.com/token";

	/// <summary>
	/// Gets or sets the URI for the authentication provider's X.509 certificates.
	/// </summary>
	[JsonPropertyName("auth_provider_x509_cert_url")]
	public string AuthProviderCertUrl { get; set; } = "https://www.googleapis.com/oauth2/v1/certs";

	/// <summary>
	/// Gets or sets the URI for the client's X.509 certificate.
	/// </summary>
	[JsonPropertyName("client_x509_cert_url")]
	public string? ClientCertUrl { get; set; }
	#endregion

	#region Alternative Credential Sources
	/// <summary>
	/// Gets or sets the path to the service account credentials JSON file.
	/// </summary>
	[JsonIgnore]
	public string? CredentialsPath { get; set; }

	/// <summary>
	/// Gets or sets the service account credentials JSON content directly.
	/// </summary>
	[JsonIgnore]
	public string? CredentialsJson { get; set; }

	/// <summary>
	/// Gets or sets the base URL of the remote service endpoint (for emulators or custom endpoints).
	/// </summary>
	[JsonIgnore]
	public string? ServiceUrl { get; set; }
	#endregion

	/// <summary>
	/// Returns true if direct credentials (ClientEmail + PrivateKey) are configured.
	/// </summary>
	[JsonIgnore]
	public bool HasDirectCredentials => !string.IsNullOrEmpty(ClientEmail) && !string.IsNullOrEmpty(PrivateKey);

	private static JsonSerializerOptions _jsonSerializerOptions = new()
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	/// <summary>
	/// Serializes this tenant's service account credentials to JSON format compatible with GCP.
	/// </summary>
	public string ToServiceAccountJson() => JsonSerializer.Serialize(this, _jsonSerializerOptions);

	/// <summary>
	/// Creates a Google Cloud service client of the specified type using this tenant's credentials.
	/// </summary>
	/// <typeparam name="T">The type of Google Cloud service client to create.</typeparam>
	/// <returns>An instance of the requested service configured with this tenant's credentials.</returns>
	/// <exception cref="NotSupportedException">Thrown when T is not a supported Google Cloud client type.</exception>
	/// <remarks>
	/// Currently supported types:
	/// <list type="bullet">
	/// <item><see cref="GoogleCredential"/></item>
	/// </list>
	/// To add support for additional types (e.g., StorageClient, BigQueryClient),
	/// add the appropriate NuGet package and extend the switch statement.
	/// </remarks>
	public T GetService<T>()
	{
		object client = typeof(T).Name switch
		{
			nameof(GoogleCredential) => (GoogleCredential)this,
			// Add more Google Cloud client types here as needed:
			// "StorageClient" => StorageClient.Create((GoogleCredential)this),
			// "BigQueryClient" => BigQueryClient.Create(ProjectId, (GoogleCredential)this),
			_ => throw new NotSupportedException($"Google Cloud client type '{typeof(T).Name}' is not supported. " +
				$"Supported types: {nameof(GoogleCredential)}. " +
				$"Add the appropriate NuGet package and extend GoogleTenant.GetService<T>() for additional types.")
		};
		return (T)client;
	}

	/// <summary>
	/// Defines an implicit conversion from a <see cref="GoogleTenant"/> instance to a <see cref="GoogleCredential"/> object.
	/// </summary>
	/// <remarks>Credentials are resolved in the following order:
	/// direct credentials (ClientEmail + PrivateKey), CredentialsJson, CredentialsPath, or Application Default Credentials (ADC).</remarks>
	/// <param name="tenant">The <see cref="GoogleTenant"/> instance containing GCP credential information to convert.</param>
	public static implicit operator GoogleCredential(GoogleTenant tenant)
	{
		// Option 1: Direct credentials on this object
		if (tenant.HasDirectCredentials)
			return GoogleCredential.FromJson(tenant.ToServiceAccountJson());

		// Option 2: Full JSON content
		if (!string.IsNullOrEmpty(tenant.CredentialsJson))
			return GoogleCredential.FromJson(tenant.CredentialsJson);

		// Option 3: JSON file path
		if (!string.IsNullOrEmpty(tenant.CredentialsPath))
			return GoogleCredential.FromFile(tenant.CredentialsPath);

		// Option 4: Application Default Credentials
		return GoogleCredential.GetApplicationDefault();
	}
}
