using Amazon.Extensions.NETCore.Setup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using nc.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Aws.Tests;

public class AwsServiceExtensionFacts
{
	[Fact]
	public void AwsExtensionService_AddNascachtAwsServices_Sets_AwsOptions()
	{
		// ARRANGE
		var config = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.Build();
		var services = new ServiceCollection()
			.AddNascachtAwsServices(config.GetSection("nc"))
			.BuildServiceProvider();


		// ASSERT

		var awsOptions = services.GetService<AWSOptions>();
		Assert.NotNull(awsOptions);
		Assert.Equal(config["nc:aws:region"], awsOptions.Region.SystemName);
		Assert.Equal(config["nc:aws:serviceurl"], awsOptions.DefaultClientConfig.ServiceURL);

		var s3Client = services.GetService<Amazon.S3.IAmazonS3>();
		var secretsManagerClient = services.GetService<Amazon.SecretsManager.IAmazonSecretsManager>();
		var lambdaClient = services.GetService<Amazon.Lambda.IAmazonLambda>();
		var dynamoDbClient = services.GetService<Amazon.DynamoDBv2.IAmazonDynamoDB>();
		var encryptionStore = services.GetService<nc.Cloud.IEncryptionStore>();
		Assert.NotNull(s3Client);
		Assert.NotNull(secretsManagerClient);
		Assert.NotNull(lambdaClient);
		Assert.NotNull(dynamoDbClient);
		Assert.NotNull(encryptionStore);
	}
}
