using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using nc.Ai.Interfaces;
using nc.Ai.OpenAI;

namespace nc.Ai;

public static partial class AiServiceExtensions
{
	public static IServiceCollection AddAiOpenAI(
		this IServiceCollection services,
		string name,
		Action<OpenAIAgent> configure)
	{
		services.Configure<OpenAIAgent>(name, configure);
		services.AddSingleton(new AgentRegistration(name, sp => sp.GetRequiredService<IChatClientFactory<OpenAIAgent>>()));
		return services.AddAiOpenAI();
	}

	public static IServiceCollection AddAiOpenAI(
		this IServiceCollection services,
		string name,
		IConfiguration configuration)
	{
		services.Configure<OpenAIAgent>(name, configuration);
		services.AddSingleton(new AgentRegistration(name, sp => sp.GetRequiredService<IChatClientFactory<OpenAIAgent>>()));
		return services.AddAiOpenAI();
	}

	private static IServiceCollection AddAiOpenAI(this IServiceCollection services)
	{
		services.TryAddSingleton<IChatClientFactory<OpenAIAgent>, OpenAIClientFactory>();
		services.TryAddSingleton<IAgentManager, AgentManager>();
		return services;
	}
}
