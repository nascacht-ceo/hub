namespace nc.Ai;

public record ConversationStoreOptions
{
	public TimeSpan SlidingExpiration { get; init; } = TimeSpan.FromHours(24);
}
