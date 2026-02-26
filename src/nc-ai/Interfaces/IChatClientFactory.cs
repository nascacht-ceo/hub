using Microsoft.Extensions.AI;

namespace nc.Ai.Interfaces;

public interface IChatClientFactory
{
	IChatClient GetAgent(string name = "");
	IEnumerable<string> GetAgentNames();
}

public interface IChatClientFactory<TAgent> : IChatClientFactory where TAgent : IAgent { }
