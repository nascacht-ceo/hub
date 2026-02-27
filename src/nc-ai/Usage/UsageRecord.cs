namespace nc.Ai;

/// <summary>
/// Captures token usage for a single non-streaming AI call, together with
/// optional metadata such as the conversation thread and custom tags.
/// </summary>
public record UsageRecord
{
	/// <summary>Gets the model identifier reported by the provider (e.g. <c>gemini-2.0-flash</c>).</summary>
	public string ModelId { get; init; } = string.Empty;

	/// <summary>Gets the number of input (prompt) tokens consumed.</summary>
	public long InputTokens { get; init; }

	/// <summary>Gets the number of output (completion) tokens generated.</summary>
	public long OutputTokens { get; init; }

	/// <summary>Gets the UTC timestamp at which the record was created. Defaults to <see cref="DateTimeOffset.UtcNow"/>.</summary>
	public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

	/// <summary>Gets the conversation thread identifier from <c>ChatOptions.ConversationId</c>, if present.</summary>
	public string? ConversationId { get; init; }

	/// <summary>
	/// Gets arbitrary key-value metadata associated with the call (e.g. <c>agent</c> name).
	/// Tags are merged from constructor-level defaults and per-call <c>ChatOptions.AdditionalProperties</c>.
	/// </summary>
	public IReadOnlyDictionary<string, object?> Tags { get; init; } = new Dictionary<string, object?>();
}
