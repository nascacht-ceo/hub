using Microsoft.Extensions.AI;

namespace nc.Ai.Interfaces;

/// <summary>
/// Persists and retrieves conversation history for a named thread.
/// </summary>
public interface IConversationStore
{
	/// <summary>Loads the conversation history for the given thread, or an empty list if none exists.</summary>
	/// <param name="threadId">The unique thread identifier.</param>
	/// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
	Task<IReadOnlyList<ChatMessage>> LoadAsync(string threadId, CancellationToken cancellationToken = default);

	/// <summary>Persists the conversation history for the given thread, replacing any existing messages.</summary>
	/// <param name="threadId">The unique thread identifier.</param>
	/// <param name="messages">The full message list to store.</param>
	/// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
	Task SaveAsync(string threadId, IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken = default);
}
