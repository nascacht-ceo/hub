using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using nc.Ai.Azure;
using nc.Ai.Interfaces;

namespace nc.Ai;

public static partial class AiServiceExtensions
{
	/// <summary>
	/// Registers a named Azure AI Foundry agent configured via a delegate and wires it into the <see cref="IAgentManager"/>.
	/// </summary>
	/// <param name="services">The service collection to add to.</param>
	/// <param name="name">The name used to retrieve this agent from <see cref="IAgentManager"/>.</param>
	/// <param name="configure">A delegate to configure the <see cref="FoundryAgent"/> options.</param>
	public static IServiceCollection AddAiFoundry(
		this IServiceCollection services,
		string name,
		Action<FoundryAgent> configure)
	{
		services.Configure<FoundryAgent>(name, configure);
		services.AddSingleton(new AgentRegistration(name, sp => sp.GetRequiredService<IChatClientFactory<FoundryAgent>>()));
		return services.AddAiFoundry();
	}

	/// <summary>
	/// Registers a named Azure AI Foundry agent configured from an <see cref="IConfiguration"/> section and wires it into the <see cref="IAgentManager"/>.
	/// </summary>
	/// <param name="services">The service collection to add to.</param>
	/// <param name="name">The name used to retrieve this agent from <see cref="IAgentManager"/>.</param>
	/// <param name="configuration">The configuration section to bind against <see cref="FoundryAgent"/>.</param>
	public static IServiceCollection AddAiFoundry(
		this IServiceCollection services,
		string name,
		IConfiguration configuration)
	{
		services.Configure<FoundryAgent>(name, configuration);
		services.AddSingleton(new AgentRegistration(name, sp => sp.GetRequiredService<IChatClientFactory<FoundryAgent>>()));
		return services.AddAiFoundry();
	}

	private static IServiceCollection AddAiFoundry(this IServiceCollection services)
	{
		services.TryAddSingleton<IChatClientFactory<FoundryAgent>, FoundryClientFactory>();
		services.TryAddSingleton<IAgentManager, AgentManager>();
		return services;
	}
}
