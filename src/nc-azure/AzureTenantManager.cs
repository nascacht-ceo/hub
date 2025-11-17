using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nc.Cloud;
using nc.Models;

namespace nc.Azure;

/// <summary>
/// Manages Azure tenants and provides functionality for adding, removing, and retrieving tenants,
/// as well as creating Azure service clients scoped to specific tenants.
/// </summary>
public class AzureTenantManager : ITenantManager<AzureTenant>
{
	private readonly AzureTenantManagerOptions _tenantOptions;
	private readonly ITenantAccessor<AzureTenant> _tenantAccessor;
	private readonly IStore<AzureTenant, string> _tenants;
	private readonly IServiceScopeFactory _serviceScopeFactory;
	private readonly ILogger<AzureTenantManager>? _logger;
	private readonly TokenCredential _defaultCredential;
	private readonly AzureTenant _defaultTenant;

	public AzureTenantManager(
		IOptions<AzureTenantManagerOptions> tenantOptions,
		ITenantAccessor<AzureTenant> tenantAccessor,
		IStore<AzureTenant, string> tenants,
		IServiceScopeFactory serviceScopeFactory,
		ILogger<AzureTenantManager>? logger = null
	)
	{
		_tenantOptions = tenantOptions.Value;
		_tenantAccessor = tenantAccessor;
		_tenants = tenants;
		_serviceScopeFactory = serviceScopeFactory;
		_logger = logger;
		_defaultCredential = new DefaultAzureCredential();
		_defaultTenant = new AzureTenant()
		{
			Name = "default"
		};
	}

	public ValueTask<AzureTenant> AddTenantAsync(AzureTenant tenant)
		=> _tenants.PostAsync(tenant);

	public Task RemoveTenantAsync(string tenantName)
		=> _tenants.DeleteAsync(tenantName);

	/// <summary>
	/// Asynchronously retrieves a TokenCredential for the specified tenant.
	/// </summary>
	/// <remarks>
	/// If a tenant name is provided and the tenant exists, the credential is configured using the
	/// options associated with that tenant. If the tenant does not exist and the configuration allows fallback,
	/// the default credential is used instead, and a warning is logged. If no tenant name is provided,
	/// the default credential is always used.
	/// </remarks>
	/// <param name="tenantName">The name of the tenant for which the credential is created. If null, the default credential is used.</param>
	/// <returns>A TokenCredential instance for the specified tenant.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if tenantName is specified but the tenant cannot be found, and the configuration is set to throw on not found.</exception>
	public async Task<TokenCredential> GetCredentialAsync(string? tenantName = null)
	{
		var credential = _defaultCredential;
		tenantName ??= _tenantAccessor.GetTenant();
		if (tenantName != null)
		{
			var tenant = await _tenants.GetAsync(tenantName);
			if (tenant == null)
			{
				if (_tenantOptions.ThrowOnNotFound)
					throw new ArgumentOutOfRangeException(nameof(tenantName), $"Tenant '{tenantName}' not found.");
				_logger?.LogWarning("Tenant '{tenantName}' not found. Using default Azure credential instead.", tenantName);
				credential = _defaultCredential;
			}
			else
			{
				credential = tenant;
			}
		}
		_logger?.LogTrace("Creating Azure TokenCredential for tenant '{TenantName}'.", tenantName ?? "<default>");
		return credential;
	}

	public async Task<T> GetServiceAsync<T>(string? tenantName = null)
		where T : class
	{
		using var scope = _serviceScopeFactory.CreateScope();
		var factory  = scope.ServiceProvider.GetService<IAzureClientFactory<T>>();
		if (factory == null)
			throw new ArgumentOutOfRangeException(nameof(T), $"No Azure client factory registered for type {typeof(T).FullName}.");

		tenantName ??= _tenantAccessor.GetTenant();
		if (tenantName != null)
		{
			var tenant = await _tenants.GetAsync(tenantName);
			if (tenant == null)
			{
				if (_tenantOptions.ThrowOnNotFound)
					throw new ArgumentOutOfRangeException(nameof(tenantName), $"Tenant '{tenantName}' not found.");
				_logger?.LogWarning("Tenant '{tenantName}' not found. Using default Azure client instead.", tenantName);
				return factory.Create(_defaultTenant);
			}
			else
			{
				_logger?.LogTrace("Creating Azure service client of type {ServiceType} for tenant '{TenantName}'.", typeof(T).Name, tenantName);
				return factory.Create(tenant);
			}
		}
		return factory.Create(_defaultTenant);
	}

	public async IAsyncEnumerable<BlobStorageAccount> DiscoverAsync(string? tenantName = null)
	{
		var armClient = await GetServiceAsync<ArmClient>(tenantName);
		var subscriptions = armClient.GetSubscriptions();
		await foreach (var subscription in subscriptions)
		{
			IEnumerable<StorageAccountResource>? accounts = null;
			try
			{
				accounts = await subscription.GetStorageAccountsAsync().ToListAsync();
			}
			catch (RequestFailedException ex)
			{
				_logger?.LogError(ex, "User does not have permission to read storage accounts from this subscription.");
				continue;
			}
			foreach (var account in accounts)
			{
				var keys = await account.GetKeysAsync().ToListAsync();

				foreach (var key in keys.OrderBy(k => k.Permissions == StorageAccountKeyPermission.Full ? 0: 1).OrderBy(k => k.CreatedOn))
				{
					var client = new BlobServiceClient($"DefaultEndpointsProtocol=https;AccountName={account.Data.Name};AccountKey={key.Value};EndpointSuffix={account.Data.PrimaryEndpoints.BlobUri.Host}", new BlobClientOptions()
					{
						Retry =
						{
							MaxRetries = 1,
							Mode = RetryMode.Exponential,
							Delay = TimeSpan.FromSeconds(1),
							MaxDelay = TimeSpan.FromSeconds(2)
						}
					});
					List<BlobContainerItem>? containers = null;
					try
					{
						containers = await client.GetBlobContainersAsync().ToListAsync();
					}
					catch (Exception ex)
					{
						_logger?.LogError(ex, "User does not have permission to read blob containers from storage account {StorageAccount}.", account.Data.Name);
						continue;
					}
					foreach (var container in containers!)
					{
						yield return new BlobStorageAccount
						{
							ConnectionString = $"DefaultEndpointsProtocol=https;AccountName={account.Data.Name};AccountKey={key.Value};EndpointSuffix={account.Data.PrimaryEndpoints.BlobUri.Host}",
							AccessKey = key.Value,
							Name = container.Name
						};
					}
					break;
				}
			}
		}
	}
}

/// <summary>
/// Options for AzureTenantManager.
/// </summary>
public class AzureTenantManagerOptions: AzureTenant
{
	public bool ThrowOnNotFound { get; set; } = true;
}

/// <summary>
/// Provides extension methods for managing Azure tenants using an ITenantManager.
/// </summary>
public static class TenantManagerExtensions
{
	public static ValueTask<AzureTenant> AddAzureTenantAsync(this ITenantManager tenantManager, AzureTenant tenant)
	{
		var azureTenantManager = tenantManager.Services.GetRequiredService<AzureTenantManager>();
		return azureTenantManager.AddTenantAsync(tenant);
	}

	public static Task RemoveAzureTenantAsync(this ITenantManager tenantManager, string tenantName)
	{
		var azureTenantManager = tenantManager.Services.GetRequiredService<AzureTenantManager>();
		return azureTenantManager.RemoveTenantAsync(tenantName);
	}
}