using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using nc.OpenApi;

namespace nc.Extensions.DependencyInjection;

public static class OpenApiServiceExtensions
{
    public static IServiceCollection AddOpenApiService(this IServiceCollection services, IConfiguration configuration, ILogger? logger = null)
    {
        return services
            .Configure<OpenApiServiceOptions>(configuration)
            .AddOpenApiService();
    }

    public static IServiceCollection AddOpenApiService(this IServiceCollection services, OpenApiServiceOptions? options = null, ILogger? logger = null)
    {
        // Register OpenAPI service
        return services
            .AddSingleton(Options.Create(options ?? new OpenApiServiceOptions()))
            .AddOpenApiService();
    }

    private static IServiceCollection AddOpenApiService(this IServiceCollection services)
    {
        // Add DistributedMemoryCache if not already registered
        if (!services.Any(s => s.ServiceType == typeof(IHttpClientFactory)))
            services.AddHttpClient();
        if (!services.Any(s => s.ServiceType == typeof(IDistributedCache)))
            services.AddDistributedMemoryCache();
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        return services.AddSingleton<OpenApiService>();
    }

}

