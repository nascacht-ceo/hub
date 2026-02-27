using nc.Ai.Interfaces;

namespace nc.Ai;

/// <summary>
/// Pairs an agent name with a deferred factory resolver so the <see cref="AgentManager"/>
/// can register agents before the DI container is fully built.
/// </summary>
internal sealed class AgentRegistration(string name, Func<IServiceProvider, IChatClientFactory> factory)
{
	/// <summary>Gets the name that uniquely identifies this agent within <see cref="AgentManager"/>.</summary>
	public string Name => name;

	/// <summary>Resolves the <see cref="IChatClientFactory"/> from the service provider.</summary>
	/// <param name="services">The application service provider.</param>
	public IChatClientFactory Resolve(IServiceProvider services) => factory(services);
}
