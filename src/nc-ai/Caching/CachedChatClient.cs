using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace nc.Ai.Caching;

/// <summary>
/// A <see cref="DelegatingChatClient"/> that intercepts messages containing
/// <see cref="CachedPromptReference"/> and transforms them using the
/// configured <see cref="ICacheStrategy"/> before forwarding to the inner client.
/// </summary>
public class CachedChatClient : DelegatingChatClient
{
	private readonly ICacheStrategy _strategy;

	public CachedChatClient(IChatClient innerClient, ICacheStrategy strategy)
		: base(innerClient)
	{
		ArgumentNullException.ThrowIfNull(strategy);
		_strategy = strategy;
	}

	public override Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		var transformed = _strategy.TransformMessages(messages);
		return base.GetResponseAsync(transformed, options, cancellationToken);
	}

	public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var transformed = _strategy.TransformMessages(messages);
		await foreach (var update in base.GetStreamingResponseAsync(
			transformed, options, cancellationToken))
		{
			yield return update;
		}
	}
}
