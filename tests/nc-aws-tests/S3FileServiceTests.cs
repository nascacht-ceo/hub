using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Aws.Tests;

[Collection("Amazon")]
public class S3FileServiceTests
{
	private readonly AmazonFixture _fixture;

	public S3FileServiceTests(AmazonFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public void IsRegisteredByAddNascachtAwsServices()
	{
		Assert.NotNull(_fixture.Services.GetServices<ICloudFileService>().OfType<S3FileService>());
	}

	[Fact]
	public async Task CreatesBucket()
	{
		// Arrange
		var service = _fixture.Services.GetRequiredService<ICloudFileService>();
		// Act
		var bucketName = $"nascacht-test-bucket-{Guid.NewGuid()}";
		var provider = await service.CreateAsync(bucketName);
		// Assert
		Assert.Equal(bucketName, provider.Name);
	}

}
