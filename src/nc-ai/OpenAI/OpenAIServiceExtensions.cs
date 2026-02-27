using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using nc.Ai.Interfaces;
using nc.Ai.OpenAI;

namespace nc.Ai;

public static partial class AiServiceExtensions
{
	/// <summary>
	/// Registers a named OpenAI agent configured via a delegate and wires it into the <see cref="IAgentManager"/>.
	/// </summary>
	/// <param name="services">The service collection to add to.</param>
	/// <param name="name">The name used to retrieve this agent from <see cref="IAgentManager"/>.</param>
	/// <param name="configure">A delegate to configure the <see cref="OpenAIAgent"/> options.</param>
	public static IServiceCollection AddAiOpenAI(
		this IServiceCollection services,
		string name,
		Action<OpenAIAgent> configure)
	{
		services.Configure<OpenAIAgent>(name, configure);
		services.AddSingleton(new AgentRegistration(name, sp => sp.GetRequiredService<IChatClientFactory<OpenAIAgent>>()));
		return services.AddAiOpenAI();
	}

	/// <summary>
	/// Registers a named OpenAI agent configured from an <see cref="IConfiguration"/> section and wires it into the <see cref="IAgentManager"/>.
	/// </summary>
	/// <param name="services">The service collection to add to.</param>
	/// <param name="name">The name used to retrieve this agent from <see cref="IAgentManager"/>.</param>
	/// <param name="configuration">The configuration section to bind against <see cref="OpenAIAgent"/>.</param>
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
