namespace nc.Ai;

/// <summary>
/// Configuration options for <see cref="RetryMiddleware"/>.
/// </summary>
public record RetryOptions
{
	/// <summary>Gets the maximum number of retry attempts after an initial failure. Defaults to 3.</summary>
	public int RetryCount { get; init; } = 3;
}
