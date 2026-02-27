namespace nc.Ai;

/// <summary>
/// Configuration options for <see cref="DistributedCacheConversationStore"/>.
/// </summary>
public record ConversationStoreOptions
{
	/// <summary>
	/// Gets the sliding cache expiry for conversation threads. Defaults to 24 hours.
	/// A thread's expiry resets on every <c>SaveAsync</c> call.
	/// </summary>
	public TimeSpan SlidingExpiration { get; init; } = TimeSpan.FromHours(24);
}
