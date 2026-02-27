using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using nc.Ai.Anthropic;
using nc.Ai.Interfaces;

namespace nc.Ai;

public static partial class AiServiceExtensions
{
	/// <summary>
	/// Registers a named Claude agent configured via a delegate and wires it into the <see cref="IAgentManager"/>.
	/// </summary>
	/// <param name="services">The service collection to add to.</param>
	/// <param name="name">The name used to retrieve this agent from <see cref="IAgentManager"/>.</param>
	/// <param name="configure">A delegate to configure the <see cref="ClaudeAgent"/> options.</param>
	public static IServiceCollection AddAiClaude(
		this IServiceCollection services,
		string name,
		Action<ClaudeAgent> configure)
	{
		services.Configure<ClaudeAgent>(name, configure);
		services.AddSingleton(new AgentRegistration(name, sp => sp.GetRequiredService<IChatClientFactory<ClaudeAgent>>()));
		return services.AddAiClaude();
	}

	/// <summary>
	/// Registers a named Claude agent configured from an <see cref="IConfiguration"/> section and wires it into the <see cref="IAgentManager"/>.
	/// </summary>
	/// <param name="services">The service collection to add to.</param>
	/// <param name="name">The name used to retrieve this agent from <see cref="IAgentManager"/>.</param>
	/// <param name="configuration">The configuration section to bind against <see cref="ClaudeAgent"/>.</param>
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
