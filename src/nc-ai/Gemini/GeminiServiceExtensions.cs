using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using nc.Ai.Gemini;
using nc.Ai.Interfaces;

namespace nc.Ai;

public static partial class AiServiceExtensions
{
	public static IServiceCollection AddAiGemini(
		this IServiceCollection services,
		string name,
		Action<GeminiAgent> configure)
	{
		services.Configure<GeminiAgent>(name, configure);
		return services.AddAiGemini();
	}

	public static IServiceCollection AddAiGemini(
		this IServiceCollection services,
		string name,
		IConfiguration configuration)
	{
		services.Configure<GeminiAgent>(name, configuration);
		return services.AddAiGemini();
	}

	private static IServiceCollection AddAiGemini(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<GeminiOptions>(configuration);
		return services.AddAiGemini();
	}

	private static IServiceCollection AddAiGemini(this IServiceCollection services, GeminiOptions options)
	{
		services.AddSingleton(Options.Create(options));
		return services.AddAiGemini();
	}

	private static IServiceCollection AddAiGemini(this IServiceCollection services)
	{
		services.TryAddSingleton<IDistributedCache, MemoryDistributedCache>();
		services.TryAddSingleton<IChatClientFactory<GeminiAgent>, GeminiClientFactory>();
		return services;
	}
}
