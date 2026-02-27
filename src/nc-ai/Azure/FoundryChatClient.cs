using Azure;
using Azure.AI.Inference;
using Azure.Core;
using Microsoft.Extensions.AI;

namespace nc.Ai.Azure;

/// <summary>
/// An <see cref="IChatClient"/> implementation for Azure AI Foundry, built on top of
/// <c>Azure.AI.Inference</c> and adapted to <c>Microsoft.Extensions.AI</c>.
/// Resolves an appropriate <c>TokenCredential</c> from the agent's authentication settings
/// and automatically downloads HTTP/HTTPS <c>UriContent</c> via <see cref="UriContentDownloader"/>.
/// </summary>
public class FoundryChatClient : DelegatingChatClient
{
	/// <summary>Initializes the client from a <see cref="FoundryAgent"/> configuration.</summary>
	/// <param name="agent">Agent settings including endpoint, model, and credentials.</param>
	public FoundryChatClient(FoundryAgent agent) : base(CreateInner(agent)) { }

	private static IChatClient CreateInner(FoundryAgent agent)
	{
		var endpoint = new Uri(agent.Endpoint!);

		ChatCompletionsClient chatClient = agent.ApiKey is { Length: > 0 } apiKey
			? new ChatCompletionsClient(endpoint, new AzureKeyCredential(apiKey), agent)
			: new ChatCompletionsClient(endpoint, (TokenCredential)agent, agent);

		return chatClient
			.AsIChatClient(agent.Model)
			.AsBuilder()
			.Use(inner => new UriContentDownloader(inner))
			.Build();
	}
}
