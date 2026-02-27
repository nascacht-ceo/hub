using Microsoft.Extensions.AI;
using nc.Ai.Interfaces;
using OpenAI.Responses;
using System.Runtime.CompilerServices;

#pragma warning disable OPENAI001

namespace nc.Ai.OpenAI;

/// <summary>
/// An <see cref="IChatClient"/> implementation for OpenAI, built on top of the OpenAI SDK and
/// adapted to <c>Microsoft.Extensions.AI</c>.
/// When <see cref="OpenAIAgent.UseExperimental"/> is <c>true</c>, uses the Responses API client
/// and advertises <see cref="INativeConversations"/> so conversation history is managed server-side.
/// Non-image URI content is automatically downloaded to inline bytes before the request is sent.
/// </summary>
public class OpenAIChatClient : DelegatingChatClient
{
	private static readonly HttpClient _http = new();
	private readonly bool _nativeConversations;

	/// <summary>Initializes the client from an <see cref="OpenAIAgent"/> configuration.</summary>
	/// <param name="agent">Agent settings including model, API key, and experimental flag.</param>
	public OpenAIChatClient(OpenAIAgent agent) : base(CreateInner(agent))
		=> _nativeConversations = agent.UseExperimental;

	/// <inheritdoc/>
	public override object? GetService(Type serviceType, object? serviceKey = null)
		=> serviceType == typeof(INativeConversations) && _nativeConversations
			? NativeConversationsMarker.Instance
			: base.GetService(serviceType, serviceKey);

	private static IChatClient CreateInner(OpenAIAgent agent) => agent.UseExperimental
		? new ResponsesClient(agent.Model, agent.ApiKey).AsIChatClient()
		: new global::OpenAI.Chat.ChatClient(agent.Model, agent.ApiKey).AsIChatClient();

	/// <inheritdoc/>
	public override async Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default) =>
		await base.GetResponseAsync(await TranslateAsync(messages, cancellationToken), options, cancellationToken);

	/// <inheritdoc/>
	public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var translated = await TranslateAsync(messages, cancellationToken);
		await foreach (var update in base.GetStreamingResponseAsync(translated, options, cancellationToken))
			yield return update;
	}

	private static async Task<IEnumerable<ChatMessage>> TranslateAsync(
		IEnumerable<ChatMessage> messages,
		CancellationToken cancellationToken)
	{
		var result = new List<ChatMessage>();
		foreach (var message in messages)
		{
			if (!message.Contents.OfType<UriContent>().Any(u => !IsImage(u.MediaType)))
			{
				result.Add(message);
				continue;
			}

			var contents = new List<AIContent>();
			foreach (var item in message.Contents)
			{
				if (item is UriContent uri && !IsImage(uri.MediaType) && uri.Uri is not null)
				{
					var bytes = await _http.GetByteArrayAsync(uri.Uri, cancellationToken);
					contents.Add(new DataContent(bytes, uri.MediaType));
				}
				else
				{
					contents.Add(item);
				}
			}
			result.Add(new ChatMessage(message.Role, contents));
		}
		return result;
	}

	private static bool IsImage(string? mediaType) =>
		mediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true;
}
