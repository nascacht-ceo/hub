using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using nc.Ai.Caching;
using nc.Ai.Gemini;

namespace nc.Ai;

public static partial class AiServiceExtensions
{
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
		services.TryAddSingleton<ICacheStrategy, GeminiCacheStrategy>();
		//services.AddTransient<GeminiChatClient>(sp =>
		//{
		//	var accessor = sp.GetRequiredService<ITenantAccessor<GoogleTenant>>();
		//	return new GeminiChatClient(accessor.CurrentTenant);
		//});
		return services;
	}
}
