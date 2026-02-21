using Microsoft.Extensions.AI;

namespace nc.Ai.Interfaces;

public interface IAgentManager
{
	public IChatClient GetChatClient(string agentName);

	public Task AddAgentAsync<TAgent>(TAgent agent) where TAgent: IAgent;

	public IEnumerable<string> GetAgentNames();
}
