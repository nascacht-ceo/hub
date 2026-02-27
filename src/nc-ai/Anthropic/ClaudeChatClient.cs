using Anthropic.SDK;
using Microsoft.Extensions.AI;

namespace nc.Ai.Anthropic;

/// <summary>
/// An <see cref="IChatClient"/> implementation for Anthropic Claude, built on top of
/// <c>Anthropic.SDK</c> and adapted to <c>Microsoft.Extensions.AI</c>.
/// Automatically downloads HTTP/HTTPS <c>UriContent</c> to inline data via <see cref="UriContentDownloader"/>.
/// </summary>
public class ClaudeChatClient : DelegatingChatClient
{
	/// <summary>Initializes the client from a <see cref="ClaudeAgent"/> configuration.</summary>
	/// <param name="agent">Agent settings including model and API key.</param>
	public ClaudeChatClient(ClaudeAgent agent) : base(CreateInner(agent)) { }

	private static IChatClient CreateInner(ClaudeAgent agent) =>
		new AnthropicClient(apiKeys: agent.ApiKey)
			.Messages
			.AsBuilder()
			.ConfigureOptions(opts => opts.ModelId ??= agent.Model)
			.Use(inner => new UriContentDownloader(inner))
			.Build();
}
