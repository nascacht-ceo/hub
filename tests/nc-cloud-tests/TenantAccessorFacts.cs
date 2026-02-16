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

	public class GetTenant : TenantAccessorFacts
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
			using (tenantAccessor.SetTenant(new MockTenant()))
			{
				Assert.Equal("MockTenant", tenantAccessor.GetTenant()?.Name);

			}
			Assert.Null(tenantAccessor.GetTenant());
		}

		[Fact]
		public void SupportsNestedTenants()
		{
			var tenantAccessor = _services.GetRequiredService<ITenantAccessor>();
			using (tenantAccessor.SetTenant(new MockTenant() { Name = "tenant1" }))
			{
				Assert.Equal("tenant1", tenantAccessor.GetTenant()?.Name);
				using (tenantAccessor.SetTenant(new MockTenant() { Name = "tenant2" }))
				{
					Assert.Equal("tenant2", tenantAccessor.GetTenant()?.Name);
				}
				Assert.Equal("tenant1", tenantAccessor.GetTenant()?.Name);
			}
			Assert.Null(tenantAccessor.GetTenant());
		}

	}

	public class MockTenant : ITenant
	{
		public string Name { get; set; } = "MockTenant";
		public string TenantId { get; set; } = Guid.NewGuid().ToString();

		public T GetService<T>()
		{
			throw new NotImplementedException();
		}
	}
}
