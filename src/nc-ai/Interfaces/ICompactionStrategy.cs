using Microsoft.Extensions.AI;

namespace nc.Ai.Interfaces;

/// <summary>
/// Reduces a conversation history to fit within a message or token budget
/// before it is forwarded to the AI provider.
/// </summary>
public interface ICompactionStrategy
{
	/// <summary>Returns a compacted view of <paramref name="messages"/>, discarding older turns as needed.</summary>
	/// <param name="messages">The full conversation history, including the latest user turn.</param>
	/// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
	ValueTask<IReadOnlyList<ChatMessage>> CompactAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken = default);
}
