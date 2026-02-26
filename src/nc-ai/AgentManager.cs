using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using nc.Ai.Interfaces;

namespace nc.Ai;

internal class AgentManager : IAgentManager
{
	private readonly IServiceProvider _services;
	private readonly Dictionary<string, IChatClientFactory> _index = new();

	public AgentManager(IServiceProvider services, IEnumerable<AgentRegistration> registrations)
	{
		_services = services;
		foreach (var reg in registrations)
			_index[reg.Name] = reg.Resolve(services);
	}

	public Task AddAgentAsync<TAgent>(TAgent agent) where TAgent : IAgent
	{
		var factory = _services.GetRequiredService<IChatClientFactory<TAgent>>();
		_index[agent.Name] = factory;
		return Task.CompletedTask;
	}

	public IChatClient GetChatClient(string agentName)
	{
		if (!_index.TryGetValue(agentName, out var factory))
			throw new KeyNotFoundException($"No agent registered with name '{agentName}'.");

		return factory.GetAgent(agentName);
	}

	public IEnumerable<string> GetAgentNames() => _index.Keys;
}
