using Microsoft.Extensions.AI;
using nc.Ai.Interfaces;
using System.Runtime.CompilerServices;
using System.Text;

namespace nc.Ai;

public sealed class ConversationChatClient : DelegatingChatClient
{
	private readonly IConversationStore _store;
	private readonly ICompactionStrategy _compaction;
	private readonly bool _nativeConversations;

	public ConversationChatClient(IChatClient inner, IConversationStore store, ICompactionStrategy compaction)
		: base(inner)
	{
		_store = store;
		_compaction = compaction;
		_nativeConversations = inner.GetService<INativeConversations>() is not null;
	}

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

public static class ConversationChatClientExtensions
{
	public static IChatClient WithConversationThreads(
		this IChatClient client,
		IConversationStore store,
		ICompactionStrategy? compaction = null)
		=> new ConversationChatClient(client, store, compaction ?? new SlidingWindowCompactionStrategy());
}
