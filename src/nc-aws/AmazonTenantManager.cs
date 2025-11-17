using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nc.Cloud;
using nc.Models;

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
	private readonly IStore<AmazonTenant, string> _tenants;
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
	/// <param name="tenants">The store used to manage tenant data, keyed by tenant identifier.</param>
	/// <param name="logger">An optional logger instance for logging diagnostic and operational messages.</param>
	public AmazonTenantManager(AWSOptions awsOptions, 
		IOptions<AmazonTenantManagerOptions> tenantOptions, 
		ITenantAccessor<AmazonTenant> tenantAccessor, 
		IStore<AmazonTenant, string> tenants,
		ILogger<AmazonTenantManager>? logger = null
	)
	{
		_awsOptions = awsOptions;
		_tenantOptions = tenantOptions.Value;
		_tenantAccessor = tenantAccessor;
		_tenants = tenants;
		_logger = logger;
	}

	/// <summary>
	/// Adds a new tenant asynchronously to the system.
	/// </summary>
	/// <param name="tenant">The <see cref="AmazonTenant"/> instance representing the tenant to be added. Cannot be <see langword="null"/>.</param>
	/// <returns>A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation. The task result contains the added
	/// <see cref="AmazonTenant"/> instance.</returns>
	public ValueTask<AmazonTenant> AddTenantAsync(AmazonTenant tenant) 
		=> _tenants.PostAsync(tenant);

	/// <summary>
	/// Removes the specified tenant from the system asynchronously.
	/// </summary>
	/// <remarks>This method deletes the tenant identified by <paramref name="tenantName"/> from the system. Ensure
	/// that the tenant name provided is valid and exists in the system before calling this method.</remarks>
	/// <param name="tenantName">The name of the tenant to remove. This value cannot be <see langword="null"/> or empty.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public Task RemoveTenantAsync(string tenantName) 
		=> _tenants.DeleteAsync(tenantName);

	/// <summary>
	/// Asynchronously retrieves an AWS service client of the specified type, optionally scoped to a tenant.
	/// </summary>
	/// <remarks>If a tenant name is provided and the tenant exists, the AWS service client is configured using the
	/// AWS options associated with that tenant. If the tenant does not exist and the configuration allows fallback, the
	/// default AWS options are used instead, and a warning is logged. If no tenant name is provided, the default AWS
	/// options are always used.</remarks>
	/// <typeparam name="T">The type of the AWS service client to retrieve. Must implement <see cref="IAmazonService"/>.</typeparam>
	/// <param name="tenantName">The name of the tenant for which the service client is created. If <see langword="null"/>, the default AWS options
	/// are used.</param>
	/// <returns>An instance of the AWS service client of type <typeparamref name="T"/>. The client is configured using the AWS
	/// options for the specified tenant, or the default AWS options if no tenant is specified.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="tenantName"/> is specified but the tenant cannot be found, and the configuration is set
	/// to throw on not found.</exception>
	public async Task<T> GetServiceAsync<T>(string? tenantName = null)
		 where T : class, IAmazonService
	{
		var options = _awsOptions;
		tenantName ??= _tenantAccessor.GetTenant();
		if (tenantName != null)
		{
			var tenant = await _tenants.GetAsync(tenantName);
			if (tenant == null)
			{
				if (_tenantOptions.ThrowOnNotFound)
					throw new ArgumentOutOfRangeException(nameof(tenantName), $"Tenant '{tenantName}' not found.");
				_logger?.LogWarning("Tenant '{tenantName}' not found. Using default AWS options instead.", tenantName);
				options = _awsOptions;
			} else
				options = tenant;
		}
		_logger?.LogTrace("Creating AWS service client of type {ServiceType} for tenant '{TenantName}'.", typeof(T).Name, tenantName ?? "<default>");
		return options.CreateServiceClient<T>();
	}
}

/// <summary>
/// Provides extension methods for managing Amazon tenants using an <see cref="ITenantManager"/>.
/// </summary>
/// <remarks>These extension methods simplify the process of adding and removing Amazon tenants by delegating the
/// operations to an <see cref="AmazonTenantManager"/> service.</remarks>
public static class TenantManagerExtensions
{
	/// <summary>
	/// Asynchronously adds a new Amazon tenant to the tenant manager.
	/// </summary>
	/// <remarks>This method retrieves an <see cref="AmazonTenantManager"/> service from the  <see
	/// cref="ITenantManager.Services"/> collection to perform the operation.</remarks>
	/// <param name="tenantManager">The <see cref="ITenantManager"/> instance used to manage tenants.</param>
	/// <param name="tenant">The <see cref="AmazonTenant"/> instance representing the tenant to be added.  This parameter cannot be <see
	/// langword="null"/>.</param>
	/// <returns>A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation.  The task result contains the added
	/// <see cref="AmazonTenant"/> instance.</returns>
	public static ValueTask<AmazonTenant> AddAmazonTenantAsync(this ITenantManager tenantManager, AmazonTenant tenant)
	{
		var amazonTenantManager = tenantManager.Services.GetRequiredService<AmazonTenantManager>();
		return amazonTenantManager.AddTenantAsync(tenant);
	}

	/// <summary>
	/// Removes an Amazon tenant with the specified name asynchronously.
	/// </summary>
	/// <remarks>This method uses the <see cref="AmazonTenantManager"/> service to remove the specified tenant.
	/// Ensure that the <see cref="AmazonTenantManager"/> service is registered in the dependency injection
	/// container.</remarks>
	/// <param name="tenantManager">The <see cref="ITenantManager"/> instance used to manage tenants.</param>
	/// <param name="tenantName">The name of the Amazon tenant to remove. This value cannot be <see langword="null"/> or empty.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public static Task RemoveAmazonTenantAsync(this ITenantManager tenantManager, string tenantName)
	{
		var amazonTenantManager = tenantManager.Services.GetRequiredService<AmazonTenantManager>();
		return amazonTenantManager.RemoveTenantAsync(tenantName);
	}
}