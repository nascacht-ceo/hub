using Microsoft.Extensions.AI;

namespace nc.Ai.Interfaces;

public interface ICompactionStrategy
{
	ValueTask<IReadOnlyList<ChatMessage>> CompactAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken = default);
}
