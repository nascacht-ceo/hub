using Azure.Core;
using Azure.Identity;
using nc.Cloud;
using System.ComponentModel.DataAnnotations;

namespace nc.Azure;

public class AzureTenant : ITenant
{
	public string? TenantId { get; set; }
	[Key]
	public required string Name { get; set; }
	public string? ServiceUrl { get; set; }

	public string? ClientId { get; set; }
	public string? ClientSecret { get; set; }
	public string? TenantDomain { get; set; }
	public string? SubscriptionId { get; set; }

	public List<BlobStorageAccount>? BlobStorageAccounts { get; set; }

	// Example: Implicit conversion to TokenCredential for Azure SDKs
	public static implicit operator TokenCredential(AzureTenant tenant)
	{
		if (!string.IsNullOrEmpty(tenant.ClientId) && !string.IsNullOrEmpty(tenant.ClientSecret) && !string.IsNullOrEmpty(tenant.TenantDomain))
		{
			return new ClientSecretCredential(tenant.TenantDomain, tenant.ClientId, tenant.ClientSecret);
		}
		// Fallback to DefaultAzureCredential if not all fields are set
		return new DefaultAzureCredential();
	}
}

public class BlobStorageAccount
{
	public string? Name { get; set; }
	public string? ConnectionString { get; set; }
	public string? AccessKey { get; set; }
}