using Azure.ResourceManager;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
namespace nc.Azure;

public interface IAzureClientFactory<T> where T : class
{
	public T Create(AzureTenant tenant);
}

public class SecretClientFactory : IAzureClientFactory<SecretClient>
{
	public SecretClient Create(AzureTenant tenant)
	{
		return new SecretClient(
			new Uri(tenant.ServiceUrl ?? throw new ArgumentNullException(nameof(tenant.ServiceUrl))),
			tenant
		);
	}
}

public class BlobServiceClientFactory : IAzureClientFactory<BlobServiceClient>
{
	public BlobServiceClient Create(AzureTenant tenant)
	{
		return new BlobServiceClient(
			new Uri(tenant.ServiceUrl ?? throw new ArgumentNullException(nameof(tenant.ServiceUrl))),
			tenant
		);
	}
}

//public class BlobContainerClientFactory : IAzureClientFactory<BlobContainerClient>
//{
//	private readonly BlobServiceClientFactory _blobServiceClientFactory = new BlobServiceClientFactory();
//	public BlobContainerClient Create(AzureTenant tenant)
//	{
//		var blobServiceClient = _blobServiceClientFactory.Create(tenant);
//		return blobServiceClient.GetBlobContainerClient(tenant.BlobStorageAccount ?? throw new ArgumentNullException(nameof(tenant.BlobStorageAccount)));
//	}
//}

public class ArmClientFactory : IAzureClientFactory<ArmClient>
{
	public ArmClient Create(AzureTenant tenant)
	{
		return new ArmClient(tenant, tenant.SubscriptionId);
	}
}