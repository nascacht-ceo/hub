using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using nc.Ai.Interfaces;
using System.Collections.Concurrent;

namespace nc.Ai.Anthropic;

/// <summary>
/// Creates and caches <see cref="ClaudeChatClient"/> instances per named agent configuration.
/// Optionally wraps each client with <see cref="InstructionsChatClient"/> based on the agent's settings.
/// </summary>
internal sealed class ClaudeClientFactory(IOptionsMonitor<ClaudeAgent> options) : IChatClientFactory<ClaudeAgent>
{
	/// <summary>The configuration path for Claude options (e.g. <c>nc:ai:anthropic</c>).</summary>
	internal const string ConfigSection = "nc:ai:anthropic";

	private readonly ConcurrentDictionary<string, IChatClient> _clients = new();

	/// <inheritdoc/>
	public IChatClient GetAgent(string name = "") =>
		_clients.GetOrAdd(name, Resolve);

	/// <inheritdoc/>
	public IEnumerable<string> GetAgentNames() => _clients.Keys;

	private IChatClient Resolve(string name)
	{
		var agent = options.Get(name);
		IChatClient client = new ClaudeChatClient(agent);

		if (agent.Instructions is { } agentInstructions)
			client = new InstructionsChatClient(client, agentInstructions);

		return client;
	}
}
