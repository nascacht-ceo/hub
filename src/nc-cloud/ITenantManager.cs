using Microsoft.Extensions.DependencyInjection;

namespace nc.Cloud;

public interface ITenantManager
{
	public ITenant GetTenant(string tenantName);
}

/// <summary>
/// Manage adding and removing tenants and getting services for tenants
/// </summary>
/// <typeparam name="TTenant">Type of ITenant to be managed.</typeparam>
/// <typeparam name="TService">Type of services to be created.</typeparam>
public interface ITenantManager<TTenant>: ITenantManager where TTenant: ITenant
{
	public ValueTask<TTenant> AddTenantAsync(TTenant tenant);

	public Task RemoveTenantAsync(string tenantName);

	public new TTenant GetTenant(string tenantName);

	ITenant ITenantManager.GetTenant(string tenantName)
	{
		return GetTenant(tenantName);
	}
}
