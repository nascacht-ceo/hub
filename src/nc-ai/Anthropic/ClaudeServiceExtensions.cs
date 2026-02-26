using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using nc.Ai.Anthropic;
using nc.Ai.Interfaces;

namespace nc.Ai;

public static partial class AiServiceExtensions
{
	public static IServiceCollection AddAiClaude(
		this IServiceCollection services,
		string name,
		Action<ClaudeAgent> configure)
	{
		services.Configure<ClaudeAgent>(name, configure);
		services.AddSingleton(new AgentRegistration(name, sp => sp.GetRequiredService<IChatClientFactory<ClaudeAgent>>()));
		return services.AddAiClaude();
	}

	public static IServiceCollection AddAiClaude(
		this IServiceCollection services,
		string name,
		IConfiguration configuration)
	{
		services.Configure<ClaudeAgent>(name, configuration);
		services.AddSingleton(new AgentRegistration(name, sp => sp.GetRequiredService<IChatClientFactory<ClaudeAgent>>()));
		return services.AddAiClaude();
	}

	private static IServiceCollection AddAiClaude(this IServiceCollection services)
	{
		services.TryAddSingleton<IChatClientFactory<ClaudeAgent>, ClaudeClientFactory>();
		services.TryAddSingleton<IAgentManager, AgentManager>();
		return services;
	}
}
