using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nc.Cloud;
using nc.Models;
using System.Collections.Concurrent;

namespace nc.Aws;

/// <summary>
/// Manages Amazon tenants and provides functionality for adding, removing, and retrieving tenants,  as well as creating
/// AWS service clients scoped to specific tenants.
/// </summary>
/// <remarks>This class is designed to work with Amazon Web Services (AWS) tenants, allowing for multi-tenant 
/// management in applications. It supports adding and removing tenants, retrieving tenant-specific  configurations, and
/// creating AWS service clients for a given tenant. If no tenant is specified,  the default AWS options are
/// used.</remarks>
public class AmazonTenantManager: ITenantManager<AmazonTenant>
{
	private readonly AWSOptions _awsOptions;
	private readonly AmazonTenantManagerOptions _tenantOptions;
	private readonly ITenantAccessor<AmazonTenant> _tenantAccessor;
	private readonly IStore<AmazonTenant, string> _tenantStore;
	private readonly ConcurrentDictionary<string, AmazonTenant> _tenants = new();
	private readonly ILogger<AmazonTenantManager>? _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="AmazonTenantManager"/> class, which manages Amazon tenants and
	/// provides functionality for accessing and storing tenant information.
	/// </summary>
	/// <remarks>This class is designed to facilitate the management of Amazon tenants, including retrieving and
	/// storing tenant-specific data. It relies on the provided AWS options for Amazon service integration and the tenant
	/// store for persistence.</remarks>
	/// <param name="awsOptions">The AWS configuration options used to interact with Amazon services.</param>
	/// <param name="tenantOptions">The configuration options specific to the <see cref="AmazonTenantManager"/>.</param>
	/// <param name="tenantAccessor">An accessor for retrieving the current tenant context.</param>
	/// <param name="tenantStore">The store used to manage tenant data, keyed by tenant identifier.</param>
	/// <param name="logger">An optional logger instance for logging diagnostic and operational messages.</param>
	public AmazonTenantManager(AWSOptions awsOptions, 
		IOptions<AmazonTenantManagerOptions> tenantOptions, 
		ITenantAccessor<AmazonTenant> tenantAccessor, 
		IStore<AmazonTenant, string> tenantStore,
		ILogger<AmazonTenantManager>? logger = null
	)
	{
		_awsOptions = awsOptions;
		_tenantOptions = tenantOptions.Value;
		_tenantAccessor = tenantAccessor;
		_tenantStore = tenantStore;
		_logger = logger;
	}

	/// <summary>
	/// Adds a new tenant asynchronously to the system.
	/// </summary>
	/// <param name="tenant">The <see cref="AmazonTenant"/> instance representing the tenant to be added. Cannot be <see langword="null"/>.</param>
	/// <returns>A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation. The task result contains the added
	/// <see cref="AmazonTenant"/> instance.</returns>
	public async ValueTask<AmazonTenant> AddTenantAsync(AmazonTenant tenant)
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
	/// <returns>The <see cref="AmazonTenant"/> instance.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if the tenant is not found and ThrowOnNotFound is true.</exception>
	public AmazonTenant GetTenant(string tenantName)
	{
		if (_tenants.TryGetValue(tenantName, out var tenant))
			return tenant;

		tenant = _tenantStore.GetAsync(tenantName).AsTask().GetAwaiter().GetResult();
		if (tenant == null)
		{
			if (_tenantOptions.ThrowOnNotFound)
				throw new ArgumentOutOfRangeException(nameof(tenantName), $"Tenant '{tenantName}' not found.");
			_logger?.LogWarning("Tenant '{TenantName}' not found.", tenantName);
			return null!;
		}
		return tenant;
	}
}