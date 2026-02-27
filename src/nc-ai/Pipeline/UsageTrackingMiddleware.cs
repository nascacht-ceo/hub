using Microsoft.Extensions.AI;
using nc.Ai.Interfaces;

namespace nc.Ai;

/// <summary>
/// <see cref="IChatClientMiddleware"/> adapter that wraps an agent's client with
/// <see cref="UsageTrackingChatClient"/>, tagging every record with the agent name.
/// When no <see cref="IUsageTracker"/> is registered in DI, this step is a no-op.
/// Register via <c>AddUsageTracking()</c>; reference in the pipeline as <see cref="PipelineStep.UsageTracking"/>.
/// </summary>
internal sealed class UsageTrackingMiddleware(IUsageTracker? tracker = null) : IChatClientMiddleware
{
	/// <inheritdoc/>
	public string Name => PipelineStep.UsageTracking;

	/// <inheritdoc/>
	public IChatClient Wrap(IChatClient inner, string agentName) =>
		tracker is not null
			? inner.WithUsageTracking(tracker, new Dictionary<string, object?> { ["agent"] = agentName })
			: inner;
}
