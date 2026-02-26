using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using nc.Ai.Interfaces;

namespace nc.Ai;

public static partial class AiServiceExtensions
{
	public const string ConfigSection = "ai";
	public static IServiceCollection AddNascachtAiServices(this IServiceCollection services, IConfiguration configuration)
	{
		var section = configuration.GetSection(ConfigSection);
		services.Configure<AiOptions>(section);
		services.AddAiGemini(section.GetSection("Gemini"));
		return services.AddAiServices();
	}

	public static IServiceCollection AddAiServices(this IServiceCollection services, AiOptions options)
	{
		services.AddSingleton(Options.Create(options));
		// services.AddAiGemini(options.Gemini);
		return services.AddAiServices();
	}

	private static IServiceCollection AddAiServices(this IServiceCollection services)
	{
		services.TryAddSingleton<IDistributedCache, MemoryDistributedCache>();
		return services;
	}

	public static IServiceCollection AddConversationThreads(this IServiceCollection services, Action<ConversationStoreOptions>? configure = null)
	{
		if (configure is not null)
			services.Configure<ConversationStoreOptions>(configure);
		services.TryAddSingleton<IConversationStore, DistributedCacheConversationStore>();
		services.TryAddSingleton<ICompactionStrategy>(sp =>
			new SlidingWindowCompactionStrategy(sp.GetService<IOptions<SlidingWindowOptions>>()?.Value));
		return services;
	}

	public static IServiceCollection AddConversationThreads(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<ConversationStoreOptions>(configuration);
		services.TryAddSingleton<IConversationStore, DistributedCacheConversationStore>();
		services.TryAddSingleton<ICompactionStrategy>(sp =>
			new SlidingWindowCompactionStrategy(sp.GetService<IOptions<SlidingWindowOptions>>()?.Value));
		return services;
	}
}
