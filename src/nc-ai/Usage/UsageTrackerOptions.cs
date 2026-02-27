namespace nc.Ai;

/// <summary>
/// Configuration options for <see cref="BackgroundUsageTracker"/>.
/// </summary>
public record UsageTrackerOptions
{
	/// <summary>
	/// Gets the maximum number of <see cref="UsageRecord"/> entries that can be queued
	/// before the oldest records are dropped. Defaults to 1000.
	/// </summary>
	public int ChannelCapacity { get; init; } = 1000;
}
