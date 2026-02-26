using Azure;
using Azure.AI.Inference;
using Azure.Core;
using Microsoft.Extensions.AI;

namespace nc.Ai.Azure;

public class FoundryChatClient : DelegatingChatClient
{
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
