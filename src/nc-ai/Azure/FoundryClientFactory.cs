using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using nc.Ai.Interfaces;
using System.Collections.Concurrent;

namespace nc.Ai.Azure;

internal sealed class FoundryClientFactory(IOptionsMonitor<FoundryAgent> options) : IChatClientFactory<FoundryAgent>
{
	internal const string ConfigSection = "nc:ai:azure";

	private readonly ConcurrentDictionary<string, IChatClient> _clients = new();

	public IChatClient GetAgent(string name = "") =>
		_clients.GetOrAdd(name, Resolve);

	public IEnumerable<string> GetAgentNames() => _clients.Keys;

	private IChatClient Resolve(string name)
	{
		var agent = options.Get(name);
		IChatClient client = new FoundryChatClient(agent);

		if (agent.Instructions is { } agentInstructions)
			client = new InstructionsChatClient(client, agentInstructions);

		return client;
	}
}
