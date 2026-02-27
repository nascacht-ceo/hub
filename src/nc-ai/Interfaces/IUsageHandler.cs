namespace nc.Ai.Interfaces;

/// <summary>
/// Processes a <see cref="UsageRecord"/> after it has been dequeued from the background channel.
/// Implement this interface to persist usage data to a database, telemetry sink, or other store.
/// </summary>
public interface IUsageHandler
{
	/// <summary>Handles a single usage record.</summary>
	/// <param name="record">The record describing a completed AI call.</param>
	/// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
	Task HandleAsync(UsageRecord record, CancellationToken cancellationToken = default);
}
