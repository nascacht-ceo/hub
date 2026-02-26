using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using nc.Ai.Azure;
using nc.Ai.Interfaces;

namespace nc.Ai;

public static partial class AiServiceExtensions
{
	public static IServiceCollection AddAiFoundry(
		this IServiceCollection services,
		string name,
		Action<FoundryAgent> configure)
	{
		services.Configure<FoundryAgent>(name, configure);
		services.AddSingleton(new AgentRegistration(name, sp => sp.GetRequiredService<IChatClientFactory<FoundryAgent>>()));
		return services.AddAiFoundry();
	}

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
