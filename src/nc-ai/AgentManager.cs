using Microsoft.Extensions.AI;
using nc.Ai.Interfaces;

namespace nc.Ai;

public class AgentManager : IAgentManager
{
	public AgentManager(IEnumerable<IChatClientFactory> factories)
	{

	}

	public Task AddAgentAsync<TAgent>(TAgent agent) where TAgent : IAgent
	{
		throw new NotImplementedException();
	}

	public IEnumerable<string> GetAgentNames()
	{
		throw new NotImplementedException();
	}

	public IChatClient GetChatClient(string agentName)
	{
		throw new NotImplementedException();
	}
}
