using Anthropic.SDK;
using Microsoft.Extensions.AI;

namespace nc.Ai.Anthropic;

public class ClaudeChatClient : DelegatingChatClient
{
	public ClaudeChatClient(ClaudeAgent agent) : base(CreateInner(agent)) { }

	private static IChatClient CreateInner(ClaudeAgent agent) =>
		new AnthropicClient(apiKeys: agent.ApiKey)
			.Messages
			.AsBuilder()
			.ConfigureOptions(opts => opts.ModelId ??= agent.Model)
			.Use(inner => new UriContentDownloader(inner))
			.Build();
}
