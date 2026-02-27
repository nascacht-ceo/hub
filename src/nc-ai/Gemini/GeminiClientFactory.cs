using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using nc.Ai.Interfaces;
using System.Collections.Concurrent;

namespace nc.Ai.Gemini;

/// <summary>
/// Creates and caches <see cref="GeminiChatClient"/> instances per named agent configuration.
/// Optionally wraps each client with <see cref="RetryChatClient"/> and
/// <see cref="InstructionsChatClient"/> based on the agent's settings.
/// </summary>
internal sealed class GeminiClientFactory(
	IOptionsMonitor<GeminiAgent> options,
	IDistributedCache cache) : IChatClientFactory<GeminiAgent>
{
	/// <summary>The configuration path for Gemini options (e.g. <c>nc:ai:gemini</c>).</summary>
	internal const string ConfigSection = "nc:ai:gemini";

	private readonly ConcurrentDictionary<string, IChatClient> _clients = new();

	/// <inheritdoc/>
	public IChatClient GetAgent(string name = "") =>
		_clients.GetOrAdd(name, Resolve);

	/// <inheritdoc/>
	public IEnumerable<string> GetAgentNames() => _clients.Keys;

	private IChatClient Resolve(string name)
	{
		var agent = options.Get(name);
		IChatClient client = new GeminiChatClient(agent, cache);

		if (agent.RetryCount > 0)
			client = new RetryChatClient(client, agent.RetryCount);

		if (agent.Instructions is { } agentInstructions)
			client = new InstructionsChatClient(client, agentInstructions);

		return client;
	}
}
