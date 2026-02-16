using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nc.Cloud;
using nc.Models;

namespace nc.Google;

/// <summary>
/// Manages Google Cloud Platform tenants and provides functionality for adding, removing, and retrieving tenants,
/// as well as creating GCP credentials scoped to specific tenants.
/// </summary>
/// <remarks>This class is designed to work with Google Cloud Platform (GCP) tenants, allowing for multi-tenant
/// management in applications. It supports adding and removing tenants, retrieving tenant-specific configurations,
/// and creating GCP credentials for a given tenant. If no tenant is specified, the default credentials (ADC) are used.</remarks>
public class GoogleTenantManager : ITenantManager<GoogleTenant>
{
	private readonly GoogleTenantManagerOptions _tenantOptions;
	private readonly ITenantAccessor<GoogleTenant> _tenantAccessor;
	private readonly IStore<GoogleTenant, string> _tenants;
	private readonly ILogger<GoogleTenantManager>? _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="GoogleTenantManager"/> class, which manages Google tenants and
	/// provides functionality for accessing and storing tenant information.
	/// </summary>
	/// <param name="tenantOptions">The configuration options specific to the <see cref="GoogleTenantManager"/>.</param>
	/// <param name="tenantAccessor">An accessor for retrieving the current tenant context.</param>
	/// <param name="tenants">The store used to manage tenant data, keyed by tenant identifier.</param>
	/// <param name="logger">An optional logger instance for logging diagnostic and operational messages.</param>
	public GoogleTenantManager(
		IOptions<GoogleTenantManagerOptions> tenantOptions,
		ITenantAccessor<GoogleTenant> tenantAccessor,
		IStore<GoogleTenant, string> tenants,
		ILogger<GoogleTenantManager>? logger = null
	)
	{
		_tenantOptions = tenantOptions.Value;
		_tenantAccessor = tenantAccessor;
		_tenants = tenants;
		_logger = logger;
	}

	/// <summary>
	/// Adds a new tenant asynchronously to the system.
	/// </summary>
	/// <param name="tenant">The <see cref="GoogleTenant"/> instance representing the tenant to be added.</param>
	/// <returns>A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation. The task result contains the added
	/// <see cref="GoogleTenant"/> instance.</returns>
	public ValueTask<GoogleTenant> AddTenantAsync(GoogleTenant tenant)
		=> _tenants.PostAsync(tenant);

	/// <summary>
	/// Removes the specified tenant from the system asynchronously.
	/// </summary>
	/// <param name="tenantName">The name of the tenant to remove.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public Task RemoveTenantAsync(string tenantName)
		=> _tenants.DeleteAsync(tenantName);

	/// <summary>
	/// Retrieves the tenant with the specified name.
	/// </summary>
	/// <param name="tenantName">The name of the tenant to retrieve.</param>
	/// <returns>The <see cref="GoogleTenant"/> instance.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if the tenant is not found and ThrowOnNotFound is true.</exception>
	public GoogleTenant GetTenant(string tenantName)
	{
		var tenant = _tenants.GetAsync(tenantName).AsTask().GetAwaiter().GetResult();
		if (tenant == null && _tenantOptions.ThrowOnNotFound)
			throw new ArgumentOutOfRangeException(nameof(tenantName), $"Tenant '{tenantName}' not found.");
		return tenant!;
	}

	/// <summary>
	/// Asynchronously retrieves a <see cref="GoogleTenant"/> for the specified tenant name.
	/// </summary>
	/// <param name="tenantName">The name of the tenant to retrieve. If null, uses the current tenant context.</param>
	/// <returns>The <see cref="GoogleTenant"/> instance, or null if not found and ThrowOnNotFound is false.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if the tenant is not found and ThrowOnNotFound is true.</exception>
	//public async Task<GoogleTenant?> GetTenantAsync(string? tenantName = null)
	//{
	//	tenantName ??= _tenantAccessor.GetTenantName();
	//	if (tenantName == null)
	//		return null;

	//	var tenant = await _tenants.GetAsync(tenantName);
	//	if (tenant == null)
	//	{
	//		if (_tenantOptions.ThrowOnNotFound)
	//			throw new ArgumentOutOfRangeException(nameof(tenantName), $"Tenant '{tenantName}' not found.");
	//		_logger?.LogWarning("Tenant '{TenantName}' not found.", tenantName);
	//	}
	//	return tenant;
	//}

	/// <summary>
	/// Asynchronously retrieves GCP credentials for the specified tenant.
	/// </summary>
	/// <remarks>If a tenant name is provided and the tenant exists, the credentials are retrieved from that tenant's
	/// configuration. If the tenant does not exist and the configuration allows fallback, Application Default Credentials
	/// are used instead, and a warning is logged.</remarks>
	/// <param name="tenantName">The name of the tenant for which credentials are retrieved. If null, ADC is used.</param>
	/// <returns>A <see cref="GoogleCredential"/> configured for the specified tenant, or ADC if no tenant is specified.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if the tenant is not found and ThrowOnNotFound is true.</exception>
	//public async Task<GoogleCredential> GetCredentialAsync(string? tenantName = null)
	//{
	//	tenantName ??= _tenantAccessor.GetTenantName();
	//	if (tenantName != null)
	//	{
	//		var tenant = await _tenants.GetAsync(tenantName);
	//		if (tenant == null)
	//		{
	//			if (_tenantOptions.ThrowOnNotFound)
	//				throw new ArgumentOutOfRangeException(nameof(tenantName), $"Tenant '{tenantName}' not found.");
	//			_logger?.LogWarning("Tenant '{TenantName}' not found. Using Application Default Credentials instead.", tenantName);
	//		}
	//		else
	//		{
	//			_logger?.LogTrace("Creating GCP credentials for tenant '{TenantName}'.", tenantName);
	//			return tenant;
	//		}
	//	}
	//	_logger?.LogTrace("Using Application Default Credentials (no tenant specified).");
	//	return GoogleCredential.GetApplicationDefault();
	//}

	/// <summary>
	/// Asynchronously retrieves the GCP project ID for the specified tenant.
	/// </summary>
	/// <param name="tenantName">The name of the tenant. If null, uses the current tenant context or default project.</param>
	/// <returns>The project ID for the tenant, or the default project ID if no tenant is found.</returns>
	//public async Task<string?> GetProjectIdAsync(string? tenantName = null)
	//{
	//	tenantName ??= _tenantAccessor.GetTenantName();
	//	if (tenantName != null)
	//	{
	//		var tenant = await _tenants.GetAsync(tenantName);
	//		if (tenant?.ProjectId != null)
	//			return tenant.ProjectId;
	//	}
	//	return _tenantOptions.DefaultProjectId;
	//}
}

// TODO: Re-enable when ITenantManager base interface with Services property is implemented
///// <summary>
///// Provides extension methods for managing Google tenants using an <see cref="ITenantManager"/>.
///// </summary>
//public static class GoogleTenantManagerExtensions
//{
//	public static ValueTask<GoogleTenant> AddGoogleTenantAsync(this ITenantManager tenantManager, GoogleTenant tenant)
//	{
//		var googleTenantManager = tenantManager.Services.GetRequiredService<GoogleTenantManager>();
//		return googleTenantManager.AddTenantAsync(tenant);
//	}
//
//	public static Task RemoveGoogleTenantAsync(this ITenantManager tenantManager, string tenantName)
//	{
//		var googleTenantManager = tenantManager.Services.GetRequiredService<GoogleTenantManager>();
//		return googleTenantManager.RemoveTenantAsync(tenantName);
//	}
//
//	public static Task<GoogleCredential> GetGoogleCredentialAsync(this ITenantManager tenantManager, string? tenantName = null)
//	{
//		var googleTenantManager = tenantManager.Services.GetRequiredService<GoogleTenantManager>();
//		return googleTenantManager.GetCredentialAsync(tenantName);
//	}
//}
