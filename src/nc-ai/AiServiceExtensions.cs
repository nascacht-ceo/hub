using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using nc.Ai.Interfaces;

namespace nc.Ai;

/// <summary>
/// Extension methods for registering Nascacht AI services in an <see cref="IServiceCollection"/>.
/// Partial class; provider-specific registrations live in their respective files.
/// </summary>
public static partial class AiServiceExtensions
{
	/// <summary>The root configuration section key for AI options (<c>ai</c>).</summary>
	public const string ConfigSection = "ai";

	/// <summary>
	/// Registers all AI services from the <c>ai</c> configuration section,
	/// including Gemini provider support.
	/// </summary>
	/// <param name="services">The service collection to add to.</param>
	/// <param name="configuration">The application configuration root.</param>
	public static IServiceCollection AddNascachtAiServices(this IServiceCollection services, IConfiguration configuration)
	{
		var section = configuration.GetSection(ConfigSection);
		services.Configure<AiOptions>(section);
		services.AddAiGemini(section.GetSection("Gemini"));
		return services.AddAiServices();
	}

	/// <summary>
	/// Registers AI services from a pre-built <see cref="AiOptions"/> instance.
	/// </summary>
	/// <param name="services">The service collection to add to.</param>
	/// <param name="options">The options instance to register.</param>
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

	/// <summary>
	/// Registers conversation thread support, backed by <see cref="DistributedCacheConversationStore"/>
	/// with a <see cref="SlidingWindowCompactionStrategy"/>.
	/// </summary>
	/// <param name="services">The service collection to add to.</param>
	/// <param name="configure">Optional delegate to configure <see cref="ConversationStoreOptions"/>.</param>
	public static IServiceCollection AddConversationThreads(this IServiceCollection services, Action<ConversationStoreOptions>? configure = null)
	{
		if (configure is not null)
			services.Configure<ConversationStoreOptions>(configure);
		services.TryAddSingleton<IConversationStore, DistributedCacheConversationStore>();
		services.TryAddSingleton<ICompactionStrategy>(sp =>
			new SlidingWindowCompactionStrategy(sp.GetService<IOptions<SlidingWindowOptions>>()?.Value));
		return services;
	}

	/// <summary>
	/// Registers conversation thread support, binding <see cref="ConversationStoreOptions"/> from
	/// <paramref name="configuration"/>.
	/// </summary>
	/// <param name="services">The service collection to add to.</param>
	/// <param name="configuration">Configuration section to bind against <see cref="ConversationStoreOptions"/>.</param>
	public static IServiceCollection AddConversationThreads(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<ConversationStoreOptions>(configuration);
		services.TryAddSingleton<IConversationStore, DistributedCacheConversationStore>();
		services.TryAddSingleton<ICompactionStrategy>(sp =>
			new SlidingWindowCompactionStrategy(sp.GetService<IOptions<SlidingWindowOptions>>()?.Value));
		return services;
	}

	/// <summary>
	/// Registers background usage tracking with a custom <typeparamref name="THandler"/> that
	/// persists or processes dequeued <see cref="UsageRecord"/> instances.
	/// Also registers <see cref="UsageTrackingMiddleware"/> for the agent pipeline.
	/// </summary>
	/// <typeparam name="THandler">The <see cref="IUsageHandler"/> implementation to use.</typeparam>
	/// <param name="services">The service collection to add to.</param>
	/// <param name="configure">Optional delegate to configure <see cref="UsageTrackerOptions"/>.</param>
	public static IServiceCollection AddUsageTracking<THandler>(
		this IServiceCollection services,
		Action<UsageTrackerOptions>? configure = null)
		where THandler : class, IUsageHandler
	{
		if (configure is not null)
			services.Configure<UsageTrackerOptions>(configure);
		services.TryAddSingleton<IUsageHandler, THandler>();
		services.AddSingleton<BackgroundUsageTracker>();
		services.AddSingleton<IUsageTracker>(sp => sp.GetRequiredService<BackgroundUsageTracker>());
		services.AddHostedService(sp => sp.GetRequiredService<BackgroundUsageTracker>());
		services.TryAddEnumerable(ServiceDescriptor.Singleton<IChatClientMiddleware, UsageTrackingMiddleware>());
		return services;
	}

	/// <summary>
	/// Registers background usage tracking using the default <see cref="LoggingUsageHandler"/>.
	/// </summary>
	/// <param name="services">The service collection to add to.</param>
	/// <param name="configure">Optional delegate to configure <see cref="UsageTrackerOptions"/>.</param>
	public static IServiceCollection AddUsageTracking(
		this IServiceCollection services,
		Action<UsageTrackerOptions>? configure = null)
		=> services.AddUsageTracking<LoggingUsageHandler>(configure);

	/// <summary>
	/// Registers the <see cref="RetryMiddleware"/> for the agent pipeline.
	/// </summary>
	/// <param name="services">The service collection to add to.</param>
	/// <param name="configure">Optional delegate to configure <see cref="RetryOptions"/>.</param>
	public static IServiceCollection AddRetry(
		this IServiceCollection services,
		Action<RetryOptions>? configure = null)
	{
		if (configure is not null)
			services.Configure<RetryOptions>(configure);
		services.TryAddEnumerable(ServiceDescriptor.Singleton<IChatClientMiddleware, RetryMiddleware>());
		return services;
	}

	/// <summary>
	/// Registers the <see cref="RetryMiddleware"/> for the agent pipeline, binding
	/// <see cref="RetryOptions"/> from <paramref name="configuration"/>.
	/// </summary>
	/// <param name="services">The service collection to add to.</param>
	/// <param name="configuration">Configuration section to bind against <see cref="RetryOptions"/>.</param>
	public static IServiceCollection AddRetry(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		services.Configure<RetryOptions>(configuration);
		services.TryAddEnumerable(ServiceDescriptor.Singleton<IChatClientMiddleware, RetryMiddleware>());
		return services;
	}
}
