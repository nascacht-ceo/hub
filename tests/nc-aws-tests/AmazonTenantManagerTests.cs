using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using nc.Cloud;

namespace nc.Aws.Tests;

[Collection((nameof(AmazonFixture)))]
public class AmazonTenantManagerTests
{
	private readonly AmazonFixture _fixture;

	public AmazonTenantManagerTests(AmazonFixture fixture)
	{
		_fixture = fixture;
	}

	//public class GetService : AmazonTenantManagerTests
	//{
	//	public GetService(AmazonFixture fixture) : base(fixture)
	//	{ }

	//	[Fact]
	//	public async Task UsesDefaultTenant()
	//	{
	//		var tenantManager = _fixture.Services.GetRequiredService<AmazonTenantManager>();
	//		var s3Client = await tenantManager.GetServiceAsync<IAmazonS3>();
	//		Assert.NotNull(s3Client);
	//		Assert.Equal(_fixture.ServiceUrl, s3Client.Config.ServiceURL);
	//	}

	//	[Fact]
	//	public async Task ThrowsOnInvalidTenant()
	//	{
	//		var tenantManager = _fixture.Services.GetRequiredService<AmazonTenantManager>();
	//		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => tenantManager.GetServiceAsync<IAmazonS3>("invalidTenant"));
	//	}

	//	[Fact]
	//	public async Task UsesCustomTenant()
	//	{
	//		var tenantManager = _fixture.Services.GetRequiredService<AmazonTenantManager>();
	//		var tenantName = $"{Guid.NewGuid()}";
	//		await tenantManager.AddTenantAsync(new AmazonTenant
	//		{
	//			TenantId = tenantName,
	//			Name = tenantName,
	//			AccessKey = "abc",
	//			SecretKey = "123",
	//			ServiceUrl = "https://custom-endpoint.local/"
	//		});
	//		var s3Client = await tenantManager.GetServiceAsync<IAmazonS3>(tenantName);
	//		Assert.NotNull(s3Client);
	//		Assert.Equal("https://custom-endpoint.local/", s3Client.Config.ServiceURL);

	//		await tenantManager.RemoveTenantAsync(tenantName);
	//	}

	//	[Fact]
	//	public async Task UsesTenantAccessor()
	//	{
	//		var tenantManager = _fixture.Services.GetRequiredService<AmazonTenantManager>();
	//		var tenantAccessor = _fixture.Services.GetRequiredService<ITenantAccessor<AmazonTenant>>();
	//		var tenantName = $"{Guid.NewGuid()}";
	//		await tenantManager.AddTenantAsync(new AmazonTenant
	//		{
	//			TenantId = tenantName,
	//			Name = tenantName,
	//			AccessKey = "bcd",
	//			SecretKey = "234",
	//			ServiceUrl = "https://other-endpoint.local/"
	//		});
	//		using (var tenantScope = tenantAccessor.SetTenantName(tenantName))
	//		{
	//			var s3Client = await tenantManager.GetServiceAsync<IAmazonS3>();
	//			Assert.NotNull(s3Client);
	//			Assert.Equal("https://other-endpoint.local/", s3Client.Config.ServiceURL);
	//		}

	//		await tenantManager.RemoveTenantAsync(tenantName);
	//	}

	//}

	public class Extensions: AmazonTenantManagerTests
	{
		public Extensions(AmazonFixture fixture) : base(fixture)
		{ }

		[Fact]
		public async Task AddTenant()
		{
			//var tenantManager = _fixture.Services.GetRequiredService<AmazonTenantManager>();
			//var tenantName = $"{Guid.NewGuid()}";
			//var tenant = new AmazonTenant
			//{
			//	TenantId = tenantName,
			//	Name = tenantName,
			//	AccessKey = "def",
			//	SecretKey = "345",
			//	ServiceUrl = "https://extension-endpoint.local/"
			//};
			//await tenantManager.AddTenantAsync(tenant);
			//var amazonTenantManager = _fixture.Services.GetRequiredService<AmazonTenantManager>();
			//var s3Client = await amazonTenantManager.GetServiceAsync<IAmazonS3>(tenantName);
			//Assert.NotNull(s3Client);
			//Assert.Equal("https://extension-endpoint.local/", s3Client.Config.ServiceURL);
			//await amazonTenantManager.RemoveTenantAsync(tenantName);
		}

		[Fact]
		public async Task RemoveTenant()
		{
			//var tenantManager = _fixture.Services.GetRequiredService<AmazonTenantManager>();
			//var tenantName = $"{Guid.NewGuid()}";
			//var tenant = new AmazonTenant
			//{
			//	TenantId = tenantName,
			//	Name = tenantName,
			//	AccessKey = "efg",
			//	SecretKey = "456",
			//	ServiceUrl = "https://remove-endpoint.local/"
			//};
			//await tenantManager.AddTenantAsync(tenant);
			//await tenantManager.RemoveTenantAsync(tenantName);
			//var amazonTenantManager = _fixture.Services.GetRequiredService<AmazonTenantManager>();
			//await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => amazonTenantManager.GetServiceAsync<IAmazonS3>(tenantName));
		}
	}
}
