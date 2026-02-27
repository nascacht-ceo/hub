using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using nc.Ai.Interfaces;
using System.Collections.Concurrent;

namespace nc.Ai.Azure;

/// <summary>
/// Creates and caches <see cref="FoundryChatClient"/> instances per named agent configuration.
/// Optionally wraps each client with <see cref="InstructionsChatClient"/> based on the agent's settings.
/// </summary>
internal sealed class FoundryClientFactory(IOptionsMonitor<FoundryAgent> options) : IChatClientFactory<FoundryAgent>
{
	/// <summary>The configuration path for Foundry options (e.g. <c>nc:ai:azure</c>).</summary>
	internal const string ConfigSection = "nc:ai:azure";

	private readonly ConcurrentDictionary<string, IChatClient> _clients = new();

	/// <inheritdoc/>
	public IChatClient GetAgent(string name = "") =>
		_clients.GetOrAdd(name, Resolve);

	/// <inheritdoc/>
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
