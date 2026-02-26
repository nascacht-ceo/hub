using Microsoft.Extensions.AI;
using nc.Ai.Interfaces;

namespace nc.Ai;

public record SlidingWindowOptions
{
	public int MaxMessages { get; init; } = 20;
}

public class SlidingWindowCompactionStrategy : ICompactionStrategy
{
	private readonly SlidingWindowOptions _options;

	public SlidingWindowCompactionStrategy(SlidingWindowOptions? options = null)
		=> _options = options ?? new();

	public ValueTask<IReadOnlyList<ChatMessage>> CompactAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken = default)
	{
		if (messages.Count <= _options.MaxMessages)
			return new(messages);

		var system = messages.Where(m => m.Role == ChatRole.System).ToList();
		var nonSystem = messages.Where(m => m.Role != ChatRole.System).ToList();
		var keepCount = Math.Max(0, _options.MaxMessages - system.Count);

		IReadOnlyList<ChatMessage> result = [..system, ..nonSystem.TakeLast(keepCount)];
		return new(result);
	}
}
