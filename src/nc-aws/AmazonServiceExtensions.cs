using Amazon.DynamoDBv2;
using Amazon.Lambda;
using Amazon.S3;
using Amazon.SecretsManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using nc.Aws;
using nc.Cloud;
using nc.Models;

namespace nc.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for configuring AWS services in an application.
/// </summary>
/// <remarks>This class contains methods to simplify the registration and configuration of AWS services  within an
/// application's dependency injection container. It is designed to integrate with  the .NET configuration system and
/// AWS SDK for .NET.</remarks>
public static class AmazonServiceExtensions
{
	/// <summary>
	/// Represents the configuration section name used to retrieve AWS-related settings.
	/// Defaults to "aws".
	/// </summary>
	/// <remarks>This constant is typically used as a key to access configuration values specific to AWS services
	/// from a configuration provider, such as appsettings.json or environment variables.</remarks>
	public const string ConfigSection = "aws";

	/// <summary>
	/// Adds AWS-related services and configurations to the specified <see cref="IServiceCollection"/>.
	/// </summary>
	/// <remarks>This method configures AWS options and services based on the application's configuration. It
	/// retrieves settings from the configuration section specified by the <c>ConfigSection</c> constant and registers them
	/// for dependency injection.</remarks>
	/// <param name="services">The <see cref="IServiceCollection"/> to which the AWS services will be added.</param>
	/// <param name="configuration">The application's configuration, used to retrieve AWS settings.</param>
	/// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
	public static IServiceCollection AddNascachtAmazonServices(this IServiceCollection services, IConfiguration configuration)
	{
		var section = configuration.GetSection(ConfigSection);
		var defaultOptions = section.GetAWSOptions(string.Empty);
		if (section["AccessKey"] is not null && section["SecretKey"] is not null)
		{
			defaultOptions.Credentials = new Amazon.Runtime.BasicAWSCredentials(
				section["AccessKey"]!,
				section["SecretKey"]!);
		}
		services.AddDefaultAWSOptions(defaultOptions);
		services.Configure<DynamoStoreOptions>(section.GetSection(nameof(DynamoStoreOptions)));
		services.Configure<EncryptionStoreOptions>(section.GetSection(nameof(EncryptionStoreOptions)));

		//if (section.GetSection("s3").GetChildren().Any())
		//{ 
		//	var s3Config = section.GetSection("s3").Get<AmazonS3Config>();
		//	services.TryAddAWSService<IAmazonS3>(s3Config, ServiceLifetime.Scoped);
		//}
		

		services.AddNascachtAwsServices();
		return services;
	}

	/// <summary>
	/// Adds AWS services and related dependencies to the specified <see cref="IServiceCollection"/>.
	/// </summary>
	/// <remarks>This method registers the following AWS services with the dependency injection container: 
	/// <list type="bullet"> 
	/// <item><description><see cref="IAmazonDynamoDB"/></description></item> 
	/// <item><description><see cref="IAmazonS3"/></description></item>
	/// <item><description><see cref="IAmazonSecretsManager"/></description></item> 
	/// <item><description><see cref="IAmazonLambda"/></description></item> 
	/// </list> 
	/// Additionally, it registers an implementation of <see cref="IEncryptionStore"/> as a singleton.
	/// </remarks>
	/// <param name="services">The <see cref="IServiceCollection"/> to which the AWS services and dependencies will be added.</param>
	/// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
	private static IServiceCollection AddNascachtAwsServices(this IServiceCollection services)
	{
		services.TryAddSingleton<ITenantAccessor<AmazonTenant>, TenantAccessor<AmazonTenant>>();
		services.TryAddSingleton<AmazonTenantManager>();
		services.AddKeyedSingleton<ITenantManager, AmazonTenantManager>("AmazonTenantManager", (sp, _) => sp.GetRequiredService<AmazonTenantManager>());
		services.TryAddAWSService<IAmazonS3>();
		services.TryAddAWSService<IAmazonSecretsManager>();
		services.TryAddAWSService<IAmazonLambda>();
		services.TryAddAWSService<IAmazonDynamoDB>();
		services.AddSingleton<IEncryptionStore, EncryptionStore>();
		services.AddScoped(typeof(IStore<,>), typeof(DynamoStore<,>));
		services.AddSingleton<IStorageProvider, S3StorageProvider>();
		services.AddSingleton<ICloudFileService, S3FileService>();	
		return services;
	}
}
