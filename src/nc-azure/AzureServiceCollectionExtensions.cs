using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using nc.Azure;

public static class AzureServiceCollectionExtensions
{
    public static IServiceCollection AddAzure(this IServiceCollection services, AzureServiceOptions options)
    {
        services.Configure<AzureServiceOptions>(o => 
        {
            o.BlobStorage = options.BlobStorage;
        });
        return services.WriteAzure();
    }

    public static IServiceCollection AddAzure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AzureServiceOptions>(configuration);
        return services.WriteAzure();
    }

    private static IServiceCollection WriteAzure(this IServiceCollection services)
    {
        services.TryAddSingleton<ICloudFileManager, CloudFileManager>();
        services.ConfigureOptions<ConfigureManagerOptions>();
        return services;
    }


}
