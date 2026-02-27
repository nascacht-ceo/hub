using Microsoft.Extensions.AI;
using nc.Ai.Interfaces;
using System.Runtime.CompilerServices;
using System.Text;

namespace nc.Ai;

/// <summary>
/// A <see cref="DelegatingChatClient"/> that maintains per-thread conversation history.
/// On each call it loads prior messages from <see cref="IConversationStore"/>, merges them
/// with the incoming messages, applies the <see cref="ICompactionStrategy"/>, and saves the
/// updated history after a response is received.
/// When the inner client advertises <see cref="INativeConversations"/>, history management
/// is bypassed and the <c>ConversationId</c> is forwarded directly to the provider.
/// </summary>
public sealed class ConversationChatClient : DelegatingChatClient
{
	private readonly IConversationStore _store;
	private readonly ICompactionStrategy _compaction;
	private readonly bool _nativeConversations;

	/// <summary>
	/// Initializes the client and detects whether the inner client handles conversations natively.
	/// </summary>
	/// <param name="inner">The underlying chat client.</param>
	/// <param name="store">The store used to persist and load conversation history.</param>
	/// <param name="compaction">The strategy used to trim history before each request.</param>
	public ConversationChatClient(IChatClient inner, IConversationStore store, ICompactionStrategy compaction)
		: base(inner)
	{
		_store = store;
		_compaction = compaction;
		_nativeConversations = inner.GetService<INativeConversations>() is not null;
	}

	/// <inheritdoc/>
	public override async Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		if (_nativeConversations)
			return await base.GetResponseAsync(messages, options, cancellationToken);

		var threadId = options?.ConversationId ?? Guid.NewGuid().ToString("N");
		var messagesToSend = await PrepareAsync(threadId, messages, cancellationToken);
		var response = await base.GetResponseAsync(messagesToSend, StripThreadId(options), cancellationToken);
		await _store.SaveAsync(threadId, [..messagesToSend, ..response.Messages], cancellationToken);
		response.ConversationId = threadId;
		return response;
	}

	/// <inheritdoc/>
	public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		if (_nativeConversations)
		{
			await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
				yield return update;
			yield break;
		}

		var threadId = options?.ConversationId ?? Guid.NewGuid().ToString("N");
		var messagesToSend = await PrepareAsync(threadId, messages, cancellationToken);
		var textBuffer = new StringBuilder();

		await foreach (var update in base.GetStreamingResponseAsync(messagesToSend, StripThreadId(options), cancellationToken))
		{
			textBuffer.Append(update.Text);
			yield return update;
		}

		await _store.SaveAsync(threadId, [..messagesToSend, new ChatMessage(ChatRole.Assistant, textBuffer.ToString())], cancellationToken);
		yield return new ChatResponseUpdate { ConversationId = threadId };
	}

	private async Task<IReadOnlyList<ChatMessage>> PrepareAsync(
		string threadId, IEnumerable<ChatMessage> incoming, CancellationToken ct)
	{
		var stored = await _store.LoadAsync(threadId, ct);
		IReadOnlyList<ChatMessage> full = [..stored, ..incoming];
		return await _compaction.CompactAsync(full, ct);
	}

	private static ChatOptions? StripThreadId(ChatOptions? options)
	{
		if (options?.ConversationId is null) return options;
		var cloned = options.Clone();
		cloned.ConversationId = null;
		return cloned;
	}
}

/// <summary>
/// Extension methods for adding conversation thread management to an <see cref="IChatClient"/>.
/// </summary>
public static class ConversationChatClientExtensions
{
	/// <summary>
	/// Wraps <paramref name="client"/> with a <see cref="ConversationChatClient"/> that automatically
	/// loads and saves thread history via <paramref name="store"/>.
	/// </summary>
	/// <param name="client">The client to wrap.</param>
	/// <param name="store">The backing store for conversation history.</param>
	/// <param name="compaction">
	/// Optional compaction strategy; defaults to <see cref="SlidingWindowCompactionStrategy"/>
	/// with its default <see cref="SlidingWindowOptions"/>.
	/// </param>
	public static IChatClient WithConversationThreads(
		this IChatClient client,
		IConversationStore store,
		ICompactionStrategy? compaction = null)
		=> new ConversationChatClient(client, store, compaction ?? new SlidingWindowCompactionStrategy());
}
