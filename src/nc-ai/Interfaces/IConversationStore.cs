using Microsoft.Extensions.AI;

namespace nc.Ai.Interfaces;

public interface IConversationStore
{
	Task<IReadOnlyList<ChatMessage>> LoadAsync(string threadId, CancellationToken cancellationToken = default);
	Task SaveAsync(string threadId, IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken = default);
}
