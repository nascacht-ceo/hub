using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using nc.Ai.Interfaces;
using System.Collections.Concurrent;

namespace nc.Ai.Gemini;

internal sealed class GeminiClientFactory(
	IOptionsMonitor<GeminiAgent> options,
	IDistributedCache cache) : IChatClientFactory<GeminiAgent>
{
	internal const string ConfigSection = "nc:ai:gemini";

	private readonly ConcurrentDictionary<string, GeminiChatClient> _clients = new();

	public IChatClient GetAgent(string name = "") =>
		_clients.GetOrAdd(name, Resolve);

	public IEnumerable<string> GetAgentNames()
	{
		return _clients.Keys;
	}

	private GeminiChatClient Resolve(string name)
	{
		var agent = options.Get(name);
		return new GeminiChatClient(agent, cache);
	}
}
