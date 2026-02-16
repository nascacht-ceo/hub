using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using nc.Cloud;

namespace nc.Azure.Tests;

[Collection(nameof(AzureFixture))]
public class AzureTenantManagerTests
{
	private readonly AzureFixture _fixture;

	public AzureTenantManagerTests(AzureFixture fixture)
	{
		_fixture = fixture;
	}

	public class Constructor: AzureTenantManagerTests
	{
		public Constructor(AzureFixture fixture) : base(fixture)
		{ }
		[Fact]
		public void SetsDefaultTenant()
		{
			var tenantManager = _fixture.Services.GetRequiredService<AzureTenantManager>();
			Assert.NotNull(tenantManager);
		}
	}

	public class DiscoverAsync: AzureTenantManagerTests
	{
		public DiscoverAsync(AzureFixture fixture) : base(fixture)
		{ }

		[Fact(Skip = "work in progress")]
		public async Task FindsStorageContainers()
		{
			var tenantManager = _fixture.Services.GetRequiredService<AzureTenantManager>();
			var containers = await tenantManager.DiscoverAsync().ToListAsync();
			Assert.NotEmpty(containers);
		}
	}

	public class Extensions : AzureTenantManagerTests
	{
		public Extensions(AzureFixture fixture) : base(fixture)
		{ }
	}
}