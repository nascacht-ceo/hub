using Microsoft.Extensions.AI;
using nc.Ai.Interfaces;

namespace nc.Ai;

/// <summary>
/// Configuration options for <see cref="SlidingWindowCompactionStrategy"/>.
/// </summary>
public record SlidingWindowOptions
{
	/// <summary>Gets the maximum number of messages to keep in the conversation history. Defaults to 20.</summary>
	public int MaxMessages { get; init; } = 20;
}

/// <summary>
/// An <see cref="ICompactionStrategy"/> that keeps system messages intact and retains
/// only the most recent non-system messages up to <see cref="SlidingWindowOptions.MaxMessages"/>.
/// </summary>
public class SlidingWindowCompactionStrategy : ICompactionStrategy
{
	private readonly SlidingWindowOptions _options;

	/// <summary>Initializes the strategy with optional custom options.</summary>
	/// <param name="options">Window size settings; defaults to <see cref="SlidingWindowOptions"/> defaults when <c>null</c>.</param>
	public SlidingWindowCompactionStrategy(SlidingWindowOptions? options = null)
		=> _options = options ?? new();

	/// <inheritdoc/>
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
