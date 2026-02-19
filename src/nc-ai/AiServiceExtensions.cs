using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using nc.Ai.Caching;

namespace nc.Ai;

public static partial class AiServiceExtensions
{
	public const string ConfigSection = "Ai";
	public static IServiceCollection AddAiServices(this IServiceCollection services, IConfiguration configuration)
	{
		var section = configuration.GetSection(ConfigSection);
		services.Configure<AiOptions>(section);
		services.AddAiGemini(section.GetSection("Gemini"));
		return services.AddAiServices();
	}

	public static IServiceCollection AddAiServices(this IServiceCollection services, AiOptions options)
	{
		services.AddSingleton(Options.Create(options));
		services.AddAiGemini(options.Gemini);
		return services.AddAiServices();
	}

	private static IServiceCollection AddAiServices(this IServiceCollection services)
	{
		services.AddSingleton<ICacheStrategy, PassthroughCacheStrategy>();
		services.TryAddSingleton<IDistributedCache, MemoryDistributedCache>();
		return services;
	}
}
