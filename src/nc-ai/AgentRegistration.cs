using nc.Ai.Interfaces;

namespace nc.Ai;

internal sealed class AgentRegistration(string name, Func<IServiceProvider, IChatClientFactory> factory)
{
	public string Name => name;
	public IChatClientFactory Resolve(IServiceProvider services) => factory(services);
}
