using Azure.ResourceManager;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using nc.Azure;
using nc.Cloud;

public static class AzureServiceExtensions
{
	public const string ConfigSection = "azure";

	public static IServiceCollection AddAzure(this IServiceCollection services, AzureServiceOptions options)
    {
        services.Configure<AzureServiceOptions>(o => 
        {
            o.BlobStorage = options.BlobStorage;
        });
        return services.AddNascachtAzureServices();
    }

    public static IServiceCollection AddNascachtAzureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(ConfigSection);
		services.Configure<AzureServiceOptions>(section);
        services.Configure<AzureTenantManagerOptions>(section);
		return services.AddNascachtAzureServices();
    }

    private static IServiceCollection AddNascachtAzureServices(this IServiceCollection services)
    {
		services.TryAddSingleton<ITenantManager, TenantManager>();
		services.TryAddSingleton<ITenantAccessor<AzureTenant>, TenantAccessor<AzureTenant>>();
		services.TryAddSingleton<AzureTenantManager>();
        services.AddSingleton<IAzureClientFactory<BlobServiceClient>, BlobServiceClientFactory>();
		services.AddSingleton<IAzureClientFactory<SecretClient>, SecretClientFactory>();
		services.AddSingleton<IAzureClientFactory<ArmClient>, ArmClientFactory>();

		services.TryAddSingleton<ICloudFileManager, CloudFileManager>();
        services.ConfigureOptions<ConfigureManagerOptions>();
        return services;
    }


}
