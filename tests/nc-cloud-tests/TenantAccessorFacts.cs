using Microsoft.Extensions.DependencyInjection;

namespace nc.Cloud.Tests;

public class TenantAccessorFacts
{
	private readonly ServiceProvider _services;

	public TenantAccessorFacts()
	{
		_services = new ServiceCollection()
			.AddSingleton<ITenantAccessor, TenantAccessor>()
			.BuildServiceProvider();
	}

	public class GetTenant: TenantAccessorFacts
	{
		[Fact]
		public void DefaultsToNull()
		{
			var tenantAccessor = _services.GetRequiredService<ITenantAccessor>();
			Assert.Null(tenantAccessor.GetTenant());
		}

		[Fact]
		public void ReturnsScopedTenant()
		{
			var tenantAccessor = _services.GetRequiredService<ITenantAccessor>();
			using (tenantAccessor.SetTenant("tenant1"))
			{
				Assert.Equal("tenant1", tenantAccessor.GetTenant());
			}
			Assert.Null(tenantAccessor.GetTenant());
		}

		[Fact]
		public void SupportsNestedTenants()
		{
			var tenantAccessor = _services.GetRequiredService<ITenantAccessor>();
			using (tenantAccessor.SetTenant("tenant1"))
			{
				Assert.Equal("tenant1", tenantAccessor.GetTenant());
				using (tenantAccessor.SetTenant("tenant2"))
				{
					Assert.Equal("tenant2", tenantAccessor.GetTenant());
				}
				Assert.Equal("tenant1", tenantAccessor.GetTenant());
			}
			Assert.Null(tenantAccessor.GetTenant());
		}

	}
}
