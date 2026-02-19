using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace nc.Ai;

/// <summary>
/// A <see cref="DelegatingChatClient"/> that intercepts messages containing
/// <see cref="Instructions"/> and resolves them to <see cref="AIContent"/> from
/// <see cref="IAiContextCache"/> before forwarding to the inner client.
/// </summary>
public class ContextCacheClient : DelegatingChatClient
{
	private readonly IAiContextCache _contextCache;

	public ContextCacheClient(IChatClient innerClient, IAiContextCache contextCache)
		: base(innerClient)
	{
		ArgumentNullException.ThrowIfNull(contextCache);
		_contextCache = contextCache;
	}

	public override async Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		if (InnerClient is IAiContextClient)
			return await base.GetResponseAsync(messages, options, cancellationToken);

		var transformed = await TransformMessagesAsync(messages, cancellationToken).ToListAsync(cancellationToken);
		return await base.GetResponseAsync(transformed, options, cancellationToken);
	}

	public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		if (InnerClient is IAiContextClient)
		{
			await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
				yield return update;
			yield break;
		}

		var transformed = await TransformMessagesAsync(messages, cancellationToken).ToListAsync(cancellationToken);
		await foreach (var update in base.GetStreamingResponseAsync(transformed, options, cancellationToken))
			yield return update;
	}

	private async IAsyncEnumerable<ChatMessage> TransformMessagesAsync(
		IEnumerable<ChatMessage> messages,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		foreach (var message in messages)
		{
			if (!message.Contents.Any(c => c is Instructions))
			{
				yield return message;
				continue;
			}

			var expanded = new List<AIContent>();
			foreach (var content in message.Contents)
			{
				if (content is Instructions aiContext)
					expanded.Add(await _contextCache.GetContextAsync(aiContext.ContextCacheKey));
				else
					expanded.Add(content);
			}

			yield return new ChatMessage(message.Role, expanded);
		}
	}
}
