using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using nc.Ai.Interfaces;
using nc.Extensions;

namespace nc.Ai;

/// <summary>
/// An <see cref="IConversationStore"/> backed by <see cref="IDistributedCache"/>,
/// serializing message history as JSON with a sliding expiry.
/// </summary>
public class DistributedCacheConversationStore : IConversationStore
{
	private readonly IDistributedCache _cache;
	private ConversationStoreOptions _options;

	/// <summary>
	/// Initializes the store and subscribes to live option changes.
	/// </summary>
	/// <param name="cache">The distributed cache backend.</param>
	/// <param name="options">Store configuration including sliding expiry duration.</param>
	public DistributedCacheConversationStore(IDistributedCache cache, IOptionsMonitor<ConversationStoreOptions> options)
	{
		_cache = cache;
		_options = options.CurrentValue;
		options.OnChange(o => _options = o);
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyList<ChatMessage>> LoadAsync(string threadId, CancellationToken cancellationToken = default)
		=> await _cache.GetAsync<List<ChatMessage>>(CacheKey(threadId), cancellationToken: cancellationToken) ?? [];

	/// <inheritdoc/>
	public Task SaveAsync(string threadId, IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken = default)
	{
		var entryOptions = new DistributedCacheEntryOptions
		{
			SlidingExpiration = _options.SlidingExpiration
		};
		return _cache.SetAsync(CacheKey(threadId), messages, entryOptions, cancellationToken: cancellationToken);
	}

	private static string CacheKey(string threadId) => $"conversation:{threadId}";
}
