using Microsoft.Extensions.AI;
using nc.Ai.Interfaces;
using System.Runtime.CompilerServices;

namespace nc.Ai;

/// <summary>
/// A <see cref="DelegatingChatClient"/> that records token usage after each non-streaming call.
/// Usage is reported via <see cref="IUsageTracker"/>, which by default enqueues records
/// into a background channel for non-blocking processing.
/// Streaming calls are passed through unchanged because MEAI v10.x does not expose
/// usage data on <c>ChatResponseUpdate</c>.
/// </summary>
public sealed class UsageTrackingChatClient : DelegatingChatClient
{
	private readonly IUsageTracker _tracker;
	private readonly IReadOnlyDictionary<string, object?> _tags;
	private readonly string _modelId;

	/// <summary>
	/// Initializes the tracking client, capturing the model ID from the inner client's metadata.
	/// </summary>
	/// <param name="inner">The underlying chat client to delegate to.</param>
	/// <param name="tracker">The tracker that receives usage records.</param>
	/// <param name="tags">Optional static tags included in every emitted record (e.g. <c>agent</c> name).</param>
	public UsageTrackingChatClient(IChatClient inner, IUsageTracker tracker, IReadOnlyDictionary<string, object?>? tags = null)
		: base(inner)
	{
		_tracker = tracker;
		_tags = tags ?? new Dictionary<string, object?>();
		_modelId = inner.GetService<ChatClientMetadata>()?.DefaultModelId ?? string.Empty;
	}

	/// <inheritdoc/>
	public override async Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		var response = await base.GetResponseAsync(messages, options, cancellationToken);
		if (response.Usage is { } usage)
			await Track(usage, options, cancellationToken);
		return response;
	}

	// Usage is not exposed on ChatResponseUpdate in MEAI v10.x streaming responses.
	// Usage tracking is only available for non-streaming calls.
	public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
			yield return update;
	}

	private ValueTask Track(UsageDetails usage, ChatOptions? options, CancellationToken ct)
	{
		var record = new UsageRecord
		{
			ModelId = _modelId,
			InputTokens = usage.InputTokenCount ?? 0,
			OutputTokens = usage.OutputTokenCount ?? 0,
			ConversationId = options?.ConversationId,
			Tags = MergeTags(options?.AdditionalProperties),
		};
		return _tracker.TrackAsync(record, ct);
	}

	private IReadOnlyDictionary<string, object?> MergeTags(AdditionalPropertiesDictionary? additional)
	{
		if (additional is null || additional.Count == 0) return _tags;
		var merged = new Dictionary<string, object?>(_tags);
		foreach (var (k, v) in additional)
			merged[k] = v;
		return merged;
	}
}

/// <summary>
/// Extension methods for adding usage tracking to an <see cref="IChatClient"/>.
/// </summary>
public static class UsageTrackingChatClientExtensions
{
	/// <summary>Wraps <paramref name="client"/> with a <see cref="UsageTrackingChatClient"/>.</summary>
	/// <param name="client">The client to wrap.</param>
	/// <param name="tracker">The tracker that receives usage records.</param>
	/// <param name="tags">Optional static tags included in every emitted record.</param>
	public static IChatClient WithUsageTracking(
		this IChatClient client,
		IUsageTracker tracker,
		IReadOnlyDictionary<string, object?>? tags = null)
		=> new UsageTrackingChatClient(client, tracker, tags);
}
