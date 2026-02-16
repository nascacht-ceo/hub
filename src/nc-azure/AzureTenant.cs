using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using nc.Cloud;
using System.ComponentModel.DataAnnotations;

namespace nc.Azure;

/// <summary>
/// Represents configuration settings for an Azure tenant, including credentials and connection details.
/// </summary>
/// <remarks>This class is typically used to encapsulate the information required to connect to Azure services on
/// behalf of a specific tenant. It supports conversion to a TokenCredential instance for use with Azure SDK clients.
/// The class includes properties for specifying client credentials, managed identity, workload identity federation,
/// and certificate-based authentication. The Name property uniquely identifies the tenant within the application.</remarks>
public class AzureTenant : ITenant
{
	/// <summary>
	/// Gets or sets the Azure AD tenant identifier (directory ID).
	/// </summary>
	public string TenantId { get; set; } = Guid.NewGuid().ToString();

	/// <summary>
	/// Gets or sets the unique name associated with the tenant.
	/// </summary>
	[Key]
	public string Name { get; set; } = "Default";

	/// <summary>
	/// Gets or sets the base URL of the Azure service endpoint.
	/// </summary>
	public string? ServiceUrl { get; set; }

	/// <summary>
	/// Gets or sets the application (client) ID for service principal authentication.
	/// </summary>
	public string? ClientId { get; set; }

	/// <summary>
	/// Gets or sets the client secret for service principal authentication.
	/// </summary>
	public string? ClientSecret { get; set; }

	/// <summary>
	/// Gets or sets the Azure AD tenant domain (e.g., "contoso.onmicrosoft.com" or tenant GUID).
	/// </summary>
	public string? TenantDomain { get; set; }

	/// <summary>
	/// Gets or sets the Azure subscription identifier.
	/// </summary>
	public string? SubscriptionId { get; set; }

	#region Certificate Authentication
	/// <summary>
	/// Gets or sets the path to the certificate file (.pfx or .pem) for certificate-based authentication.
	/// </summary>
	public string? CertificatePath { get; set; }

	/// <summary>
	/// Gets or sets the certificate thumbprint for certificate-based authentication from the certificate store.
	/// </summary>
	public string? CertificateThumbprint { get; set; }

	/// <summary>
	/// Gets or sets the password for the certificate file, if required.
	/// </summary>
	public string? CertificatePassword { get; set; }
	#endregion

	#region Workload Identity Federation (e.g., GitHub Actions, Kubernetes)
	/// <summary>
	/// Gets or sets the path to the federated token file.
	/// Used by GitHub Actions via AZURE_FEDERATED_TOKEN_FILE environment variable.
	/// </summary>
	public string? FederatedTokenFile { get; set; }

	/// <summary>
	/// Gets or sets the federated token directly (alternative to file path).
	/// </summary>
	public string? FederatedToken { get; set; }

	/// <summary>
	/// Gets or sets the assertion for client assertion credential.
	/// </summary>
	public string? ClientAssertion { get; set; }
	#endregion

	#region Managed Identity
	/// <summary>
	/// Gets or sets a value indicating whether to use managed identity for authentication.
	/// </summary>
	public bool UseManagedIdentity { get; set; }

	/// <summary>
	/// Gets or sets the resource ID for user-assigned managed identity.
	/// If not specified and UseManagedIdentity is true, system-assigned managed identity is used.
	/// </summary>
	public string? ManagedIdentityResourceId { get; set; }
	#endregion

	/// <summary>
	/// Gets or sets the list of blob storage accounts associated with this tenant.
	/// </summary>
	public List<BlobStorageAccount>? BlobStorageAccounts { get; set; }

	/// <summary>
	/// Creates an Azure service client of the specified type using this tenant's credentials.
	/// </summary>
	/// <typeparam name="T">The type of Azure service client to create.</typeparam>
	/// <returns>An instance of the requested Azure service client configured with this tenant's credentials.</returns>
	/// <exception cref="NotSupportedException">Thrown when T is not a supported Azure client type.</exception>
	public T GetService<T>()
	{
		object client = typeof(T).Name switch
		{
			nameof(BlobServiceClient) => new BlobServiceClient(new Uri(ServiceUrl!), this),
			nameof(SecretClient) => new SecretClient(new Uri(ServiceUrl!), this),
			nameof(ArmClient) => new ArmClient(this, SubscriptionId),
			_ => throw new NotSupportedException($"Azure client type '{typeof(T).Name}' is not supported. " +
				$"Supported types: {nameof(BlobServiceClient)}, {nameof(SecretClient)}, {nameof(ArmClient)}")
		};
		return (T)client;
	}

	/// <summary>
	/// Defines an implicit conversion from an <see cref="AzureTenant"/> instance to a <see cref="TokenCredential"/> object,
	/// allowing seamless use of tenant configuration with Azure SDK clients.
	/// </summary>
	/// <remarks>
	/// <para>This operator enables direct assignment of an <see cref="AzureTenant"/> to a <see cref="TokenCredential"/>
	/// variable, simplifying integration with Azure SDK clients.</para>
	/// <para>The credential priority chain is:</para>
	/// <list type="number">
	/// <item><description>Workload Identity Federation (federated token file or token)</description></item>
	/// <item><description>Client Assertion (for advanced federated scenarios)</description></item>
	/// <item><description>Certificate-based authentication (file path or thumbprint)</description></item>
	/// <item><description>Managed Identity (user-assigned or system-assigned)</description></item>
	/// <item><description>Client Secret credentials (service principal with secret)</description></item>
	/// <item><description>DefaultAzureCredential (fallback to environment, managed identity, Azure CLI, etc.)</description></item>
	/// </list>
	/// </remarks>
	/// <param name="tenant">The <see cref="AzureTenant"/> instance containing Azure credential information to convert.</param>
	public static implicit operator TokenCredential(AzureTenant tenant)
	{
		// Priority 1: Workload Identity Federation (e.g., GitHub Actions, Kubernetes)
		if (!string.IsNullOrEmpty(tenant.ClientId) && !string.IsNullOrEmpty(tenant.TenantDomain))
		{
			if (!string.IsNullOrEmpty(tenant.FederatedTokenFile) || !string.IsNullOrEmpty(tenant.FederatedToken))
			{
				var tokenFile = tenant.FederatedTokenFile ?? WriteTokenToTempFile(tenant.FederatedToken!);
				return new WorkloadIdentityCredential(new WorkloadIdentityCredentialOptions
				{
					TenantId = tenant.TenantDomain,
					ClientId = tenant.ClientId,
					TokenFilePath = tokenFile
				});
			}

			// Priority 2: Client Assertion (for advanced federated scenarios)
			if (!string.IsNullOrEmpty(tenant.ClientAssertion))
			{
				return new ClientAssertionCredential(
					tenant.TenantDomain,
					tenant.ClientId,
					() => tenant.ClientAssertion);
			}

			// Priority 3: Certificate-based authentication
			if (!string.IsNullOrEmpty(tenant.CertificatePath))
			{
				return new ClientCertificateCredential(
					tenant.TenantDomain,
					tenant.ClientId,
					tenant.CertificatePath);
			}
		}

		// Priority 4: Managed Identity
		if (tenant.UseManagedIdentity)
		{
			if (!string.IsNullOrEmpty(tenant.ManagedIdentityResourceId))
			{
				return new ManagedIdentityCredential(new ResourceIdentifier(tenant.ManagedIdentityResourceId));
			}
			else if (!string.IsNullOrEmpty(tenant.ClientId))
			{
				// User-assigned managed identity by client ID
				return new ManagedIdentityCredential(tenant.ClientId);
			}
			// System-assigned managed identity
			return new ManagedIdentityCredential();
		}

		// Priority 5: Client Secret credentials (service principal)
		if (!string.IsNullOrEmpty(tenant.ClientId) &&
			!string.IsNullOrEmpty(tenant.ClientSecret) &&
			!string.IsNullOrEmpty(tenant.TenantDomain))
		{
			return new ClientSecretCredential(tenant.TenantDomain, tenant.ClientId, tenant.ClientSecret);
		}

		// Priority 6: Fall back to DefaultAzureCredential
		// This handles environment variables, managed identity, Azure CLI, Visual Studio, etc.
		return new DefaultAzureCredential();
	}

	private static string WriteTokenToTempFile(string token)
	{
		var path = Path.GetTempFileName();
		File.WriteAllText(path, token);
		return path;
	}
}

public class BlobStorageAccount
{
	public string? Name { get; set; }
	public string? ConnectionString { get; set; }
	public string? AccessKey { get; set; }
}