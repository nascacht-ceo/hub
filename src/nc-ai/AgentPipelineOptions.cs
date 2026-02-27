namespace nc.Ai;

/// <summary>
/// Controls the <see cref="AgentManager"/> middleware pipeline applied to every agent.
/// </summary>
public record AgentPipelineOptions
{
	/// <summary>
	/// Middleware names in outermost-first order.
	/// Empty means apply all registered middleware in DI registration order.
	/// </summary>
	public IList<string> Pipeline { get; init; } = [];
}

/// <summary>
/// Well-known <see cref="IChatClientMiddleware"/> names used in <see cref="AgentPipelineOptions.Pipeline"/>.
/// </summary>
public static class PipelineStep
{
	/// <summary>Name of the <see cref="UsageTrackingMiddleware"/> step.</summary>
	public const string UsageTracking = "usage";

	/// <summary>Name of the <see cref="RetryMiddleware"/> step.</summary>
	public const string Retry = "retry";
}
