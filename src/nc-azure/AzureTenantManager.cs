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
using System.Collections.Concurrent;

namespace nc.Azure;

/// <summary>
/// Manages Azure tenants and provides functionality for adding, removing, and retrieving tenants,
/// as well as creating Azure service clients scoped to specific tenants.
/// </summary>
/// <remarks>This class is designed to work with Azure tenants, allowing for multi-tenant
/// management in applications. It supports adding and removing tenants, retrieving tenant-specific configurations, and
/// creating Azure service clients for a given tenant. If no tenant is specified, the default Azure credentials are
/// used.</remarks>
public class AzureTenantManager : ITenantManager<AzureTenant>
{
	private readonly AzureTenantManagerOptions _tenantOptions;
	private readonly ITenantAccessor<AzureTenant> _tenantAccessor;
	private readonly IStore<AzureTenant, string> _tenantStore;
	private readonly ConcurrentDictionary<string, AzureTenant> _tenants = new();
	private readonly IServiceScopeFactory _serviceScopeFactory;
	private readonly ILogger<AzureTenantManager>? _logger;
	private readonly TokenCredential _defaultCredential;
	private readonly AzureTenant _defaultTenant;

	/// <summary>
	/// Initializes a new instance of the <see cref="AzureTenantManager"/> class, which manages Azure tenants and
	/// provides functionality for accessing and storing tenant information.
	/// </summary>
	/// <remarks>This class is designed to facilitate the management of Azure tenants, including retrieving and
	/// storing tenant-specific data. It relies on the provided options for Azure service integration and the tenant
	/// store for persistence.</remarks>
	/// <param name="tenantOptions">The configuration options specific to the <see cref="AzureTenantManager"/>.</param>
	/// <param name="tenantAccessor">An accessor for retrieving the current tenant context.</param>
	/// <param name="tenantStore">The store used to manage tenant data, keyed by tenant identifier.</param>
	/// <param name="serviceScopeFactory">A factory for creating service scopes.</param>
	/// <param name="logger">An optional logger instance for logging diagnostic and operational messages.</param>
	public AzureTenantManager(
		IOptions<AzureTenantManagerOptions> tenantOptions,
		ITenantAccessor<AzureTenant> tenantAccessor,
		IStore<AzureTenant, string> tenantStore,
		IServiceScopeFactory serviceScopeFactory,
		ILogger<AzureTenantManager>? logger = null
	)
	{
		_tenantOptions = tenantOptions.Value;
		_tenantAccessor = tenantAccessor;
		_tenantStore = tenantStore;
		_serviceScopeFactory = serviceScopeFactory;
		_logger = logger;
		_defaultCredential = new DefaultAzureCredential();
		_defaultTenant = new AzureTenant()
		{
			Name = "default",
			TenantId = _tenantOptions.TenantId,
			ServiceUrl = _tenantOptions.ServiceUrl,
		};
	}

	/// <summary>
	/// Adds a new tenant asynchronously to the system.
	/// </summary>
	/// <param name="tenant">The <see cref="AzureTenant"/> instance representing the tenant to be added. Cannot be <see langword="null"/>.</param>
	/// <returns>A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation. The task result contains the added
	/// <see cref="AzureTenant"/> instance.</returns>
	public async ValueTask<AzureTenant> AddTenantAsync(AzureTenant tenant)
	{
		var result = await _tenantStore.PostAsync(tenant);
		_tenants[result.Name] = result;
		return result;
	}

	/// <summary>
	/// Removes the specified tenant from the system asynchronously.
	/// </summary>
	/// <remarks>This method deletes the tenant identified by <paramref name="tenantName"/> from the system. Ensure
	/// that the tenant name provided is valid and exists in the system before calling this method.</remarks>
	/// <param name="tenantName">The name of the tenant to remove. This value cannot be <see langword="null"/> or empty.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public async Task RemoveTenantAsync(string tenantName)
	{
		await _tenantStore.DeleteAsync(tenantName);
		_tenants.TryRemove(tenantName, out _);
	}

	/// <summary>
	/// Retrieves the tenant with the specified name.
	/// </summary>
	/// <remarks>This method first checks the in-memory cache, then falls back to the store if not found.
	/// If found in the store, the tenant is cached for future lookups.</remarks>
	/// <param name="tenantName">The name of the tenant to retrieve.</param>
	/// <returns>The <see cref="AzureTenant"/> instance.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if the tenant is not found and ThrowOnNotFound is true.</exception>
	public AzureTenant GetTenant(string tenantName)
	{
		if (_tenants.TryGetValue(tenantName, out var tenant))
			return tenant;

		// Fall back to store
		tenant = _tenantStore.GetAsync(tenantName).AsTask().GetAwaiter().GetResult();
		if (tenant == null)
		{
			if (_tenantOptions.ThrowOnNotFound)
				throw new ArgumentOutOfRangeException(nameof(tenantName), $"Tenant '{tenantName}' not found.");
			_logger?.LogWarning("Tenant '{TenantName}' not found. Using default tenant.", tenantName);
			return _defaultTenant;
		}

		// Cache it for future lookups
		_tenants[tenant.Name] = tenant;
		return tenant;
	}

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
	//public async Task<TokenCredential> GetCredentialAsync(string? tenantName = null)
	//{
	//	var credential = _defaultCredential;
	//	tenantName ??= _tenantAccessor.GetTenantName();
	//	if (tenantName != null)
	//	{
	//		var tenant = await _tenants.GetAsync(tenantName);
	//		if (tenant == null)
	//		{
	//			if (_tenantOptions.ThrowOnNotFound)
	//				throw new ArgumentOutOfRangeException(nameof(tenantName), $"Tenant '{tenantName}' not found.");
	//			_logger?.LogWarning("Tenant '{tenantName}' not found. Using default Azure credential instead.", tenantName);
	//			credential = _defaultCredential;
	//		}
	//		else
	//		{
	//			credential = tenant;
	//		}
	//	}
	//	_logger?.LogTrace("Creating Azure TokenCredential for tenant '{TenantName}'.", tenantName ?? "<default>");
	//	return credential;
	//}

	//public async Task<T> GetServiceAsync<T>(string? tenantName = null)
	//	where T : class
	//{
	//	using var scope = _serviceScopeFactory.CreateScope();
	//	var factory  = scope.ServiceProvider.GetService<IAzureClientFactory<T>>();
	//	if (factory == null)
	//		throw new ArgumentOutOfRangeException(nameof(T), $"No Azure client factory registered for type {typeof(T).FullName}.");

	//	tenantName ??= _tenantAccessor.GetTenantName();
	//	if (tenantName != null)
	//	{
	//		var tenant = await _tenants.GetAsync(tenantName);
	//		if (tenant == null)
	//		{
	//			if (_tenantOptions.ThrowOnNotFound)
	//				throw new ArgumentOutOfRangeException(nameof(tenantName), $"Tenant '{tenantName}' not found.");
	//			_logger?.LogWarning("Tenant '{tenantName}' not found. Using default Azure client instead.", tenantName);
	//			return factory.Create(_defaultTenant);
	//		}
	//		else
	//		{
	//			_logger?.LogTrace("Creating Azure service client of type {ServiceType} for tenant '{TenantName}'.", typeof(T).Name, tenantName);
	//			return factory.Create(tenant);
	//		}
	//	}
	//	return factory.Create(_defaultTenant);
	//}

	public IAsyncEnumerable<BlobStorageAccount> DiscoverAsync(string? tenantName = null)
	{
		throw new NotImplementedException();
		//var armClient = await GetServiceAsync<ArmClient>(tenantName);
		//var subscriptions = armClient.GetSubscriptions();
		//await foreach (var subscription in subscriptions)
		//{
		//	IEnumerable<StorageAccountResource>? accounts = null;
		//	try
		//	{
		//		accounts = await subscription.GetStorageAccountsAsync().ToListAsync();
		//	}
		//	catch (RequestFailedException ex)
		//	{
		//		_logger?.LogError(ex, "User does not have permission to read storage accounts from this subscription.");
		//		continue;
		//	}
		//	foreach (var account in accounts)
		//	{
		//		var keys = await account.GetKeysAsync().ToListAsync();

		//		foreach (var key in keys.OrderBy(k => k.Permissions == StorageAccountKeyPermission.Full ? 0: 1).OrderBy(k => k.CreatedOn))
		//		{
		//			var client = new BlobServiceClient($"DefaultEndpointsProtocol=https;AccountName={account.Data.Name};AccountKey={key.Value};EndpointSuffix={account.Data.PrimaryEndpoints.BlobUri.Host}", new BlobClientOptions()
		//			{
		//				Retry =
		//				{
		//					MaxRetries = 1,
		//					Mode = RetryMode.Exponential,
		//					Delay = TimeSpan.FromSeconds(1),
		//					MaxDelay = TimeSpan.FromSeconds(2)
		//				}
		//			});
		//			List<BlobContainerItem>? containers = null;
		//			try
		//			{
		//				containers = await client.GetBlobContainersAsync().ToListAsync();
		//			}
		//			catch (Exception ex)
		//			{
		//				_logger?.LogError(ex, "User does not have permission to read blob containers from storage account {StorageAccount}.", account.Data.Name);
		//				continue;
		//			}
		//			foreach (var container in containers!)
		//			{
		//				yield return new BlobStorageAccount
		//				{
		//					ConnectionString = $"DefaultEndpointsProtocol=https;AccountName={account.Data.Name};AccountKey={key.Value};EndpointSuffix={account.Data.PrimaryEndpoints.BlobUri.Host}",
		//					AccessKey = key.Value,
		//					Name = container.Name
		//				};
		//			}
		//			break;
		//		}
		//	}
		//}
	}
}

/// <summary>
/// Options for AzureTenantManager.
/// </summary>
public class AzureTenantManagerOptions: AzureTenant
{
	public bool ThrowOnNotFound { get; set; } = true;
}

// TODO: Re-enable when ITenantManager base interface with Services property is implemented
///// <summary>
///// Provides extension methods for managing Azure tenants using an ITenantManager.
///// </summary>
//public static class AzureTenantManagerExtensions
//{
//	public static ValueTask<AzureTenant> AddAzureTenantAsync(this ITenantManager tenantManager, AzureTenant tenant)
//	{
//		var azureTenantManager = tenantManager.Services.GetRequiredService<AzureTenantManager>();
//		return azureTenantManager.AddTenantAsync(tenant);
//	}
//
//	public static Task RemoveAzureTenantAsync(this ITenantManager tenantManager, string tenantName)
//	{
//		var azureTenantManager = tenantManager.Services.GetRequiredService<AzureTenantManager>();
//		return azureTenantManager.RemoveTenantAsync(tenantName);
//	}
//}