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

	public class GetService : AzureTenantManagerTests
	{
		public GetService(AzureFixture fixture) : base(fixture)
		{ }

		[Fact]
		public async Task UsesDefaultTenant()
		{
			var tenantManager = _fixture.Services.GetRequiredService<AzureTenantManager>();

			var blobClient = await tenantManager.GetServiceAsync<BlobServiceClient>();
			Assert.NotNull(blobClient);
			Assert.Equal(_fixture.Configuration["azure:serviceurl"], blobClient.Uri.ToString());
		}

		[Fact]
		public async Task ThrowsOnInvalidTenant()
		{
			var tenantManager = _fixture.Services.GetRequiredService<AzureTenantManager>();
			await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => tenantManager.GetCredentialAsync("invalidTenant"));
		}

		[Fact]
		public async Task UsesCustomTenant()
		{
			var tenantManager = _fixture.Services.GetRequiredService<AzureTenantManager>();
			var tenantName = $"{Guid.NewGuid()}";
			await tenantManager.AddTenantAsync(new AzureTenant
			{
				TenantId = tenantName,
				Name = tenantName,
				ClientId = "client-id",
				ClientSecret = "client-secret",
				TenantDomain = "tenant-domain",
				ServiceUrl = "https://custom-azure-endpoint.local/"
			});
			var blobClient = await tenantManager.GetServiceAsync<BlobServiceClient>(tenantName);
			Assert.NotNull(blobClient);
			Assert.Equal("https://custom-azure-endpoint.local/", blobClient.Uri.ToString());

			await tenantManager.RemoveTenantAsync(tenantName);
		}

		[Fact]
		public async Task UsesTenantAccessor()
		{
			var tenantManager = _fixture.Services.GetRequiredService<AzureTenantManager>();
			var tenantAccessor = _fixture.Services.GetRequiredService<ITenantAccessor<AzureTenant>>();
			var tenantName = $"{Guid.NewGuid()}";
			await tenantManager.AddTenantAsync(new AzureTenant
			{
				TenantId = tenantName,
				Name = tenantName,
				ClientId = "client-id-2",
				ClientSecret = "client-secret-2",
				TenantDomain = "tenant-domain-2",
				ServiceUrl = "https://other-azure-endpoint.local/"
			});
			using (var tenantScope = tenantAccessor.SetTenant(tenantName))
			{
				var blobClient = await tenantManager.GetServiceAsync<BlobServiceClient>();
				Assert.NotNull(blobClient);
				Assert.Equal("https://other-azure-endpoint.local/", blobClient.Uri.ToString());
			}

			await tenantManager.RemoveTenantAsync(tenantName);
		}
	}

	public class DiscoverAsync: AzureTenantManagerTests
	{
		public DiscoverAsync(AzureFixture fixture) : base(fixture)
		{ }

		[Fact]
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

		[Fact]
		public async Task AddTenant()
		{
			var tenantManager = _fixture.Services.GetRequiredService<ITenantManager>();
			var tenantName = $"{Guid.NewGuid()}";
			var tenant = new AzureTenant
			{
				TenantId = tenantName,
				Name = tenantName,
				ClientId = "client-id-3",
				ClientSecret = "client-secret-3",
				TenantDomain = "tenant-domain-3",
				ServiceUrl = "https://extension-azure-endpoint.local/"
			};
			await tenantManager.AddAzureTenantAsync(tenant);
			var azureTenantManager = _fixture.Services.GetRequiredService<AzureTenantManager>();
			var blobClient = await azureTenantManager.GetServiceAsync<BlobServiceClient>(tenantName);
			Assert.NotNull(blobClient);
			Assert.Equal("https://extension-azure-endpoint.local/", blobClient.Uri.ToString());
			await azureTenantManager.RemoveTenantAsync(tenantName);
		}

		[Fact]
		public async Task RemoveTenant()
		{
			var tenantManager = _fixture.Services.GetRequiredService<ITenantManager>();
			var tenantName = $"{Guid.NewGuid()}";
			var tenant = new AzureTenant
			{
				TenantId = tenantName,
				Name = tenantName,
				ClientId = "client-id-4",
				ClientSecret = "client-secret-4",
				TenantDomain = "tenant-domain-4",
				ServiceUrl = "https://remove-azure-endpoint.local/"
			};
			await tenantManager.AddAzureTenantAsync(tenant);
			await tenantManager.RemoveAzureTenantAsync(tenantName);
			var azureTenantManager = _fixture.Services.GetRequiredService<AzureTenantManager>();
			await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => azureTenantManager.GetCredentialAsync(tenantName));
		}
	}
}