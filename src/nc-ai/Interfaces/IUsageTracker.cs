namespace nc.Ai.Interfaces;

/// <summary>
/// Fire-and-forget sink for AI usage records. The default implementation
/// (<see cref="BackgroundUsageTracker"/>) enqueues records into a bounded channel
/// so the calling path is never blocked.
/// </summary>
public interface IUsageTracker
{
	/// <summary>Enqueues a usage record for asynchronous processing. Should not block the caller.</summary>
	/// <param name="record">The record to track.</param>
	/// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
	ValueTask TrackAsync(UsageRecord record, CancellationToken cancellationToken = default);
}
