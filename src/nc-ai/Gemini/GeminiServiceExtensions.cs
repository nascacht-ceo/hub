using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
		return services;
	}
}
