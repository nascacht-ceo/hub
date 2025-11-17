using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nc.OpenApi;

namespace nc.Extensions.DependencyInjection;

public static class OpenApiServiceExtensions
{
	public const string ConfigSection = "openapi";

	public static IServiceCollection AddNascachtOpenApiService(this IServiceCollection services, IConfiguration configuration, ILogger? logger = null)
    {
		var section = configuration.GetSection(ConfigSection);
        services.Configure<OpenApiServiceOptions>(section);
        services.AddNascachtOpenApiService();
        return services;
    }

    public static IServiceCollection AddNascachtOpenApiService(this IServiceCollection services, OpenApiServiceOptions? options = null, ILogger? logger = null)
    {
        // Register OpenAPI service
        return services
            .AddSingleton(Options.Create(options ?? new OpenApiServiceOptions()))
            .AddNascachtOpenApiService();
    }

    private static IServiceCollection AddNascachtOpenApiService(this IServiceCollection services)
    {
		// Add IHttpClientFactory if not already registered
		if (!services.Any(s => s.ServiceType == typeof(IHttpClientFactory)))
            services.AddHttpClient();
		// Add IDistributedCache if not already registered
		if (!services.Any(s => s.ServiceType == typeof(IDistributedCache)))
            services.AddDistributedMemoryCache();

        services.ConfigureOptions<OpenApiConfigureOptions>();
		services.AddLocalization();
        return services.AddSingleton<OpenApiService>();
    }

}

