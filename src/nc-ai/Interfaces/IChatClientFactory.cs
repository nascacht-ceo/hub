using Microsoft.Extensions.AI;

namespace nc.Ai.Interfaces;

public interface IChatClientFactory
{ 
	public IEnumerable<string> GetAgentNames();
}

public interface IChatClientFactory<TAgent>: IChatClientFactory where TAgent : IAgent
{
	public IChatClient GetAgent(string name = "");
}
