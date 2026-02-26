using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using nc.Ai.Interfaces;
using nc.Extensions;

namespace nc.Ai;

public class DistributedCacheConversationStore : IConversationStore
{
	private readonly IDistributedCache _cache;
	private ConversationStoreOptions _options;

	public DistributedCacheConversationStore(IDistributedCache cache, IOptionsMonitor<ConversationStoreOptions> options)
	{
		_cache = cache;
		_options = options.CurrentValue;
		options.OnChange(o => _options = o);
	}

	public async Task<IReadOnlyList<ChatMessage>> LoadAsync(string threadId, CancellationToken cancellationToken = default)
		=> await _cache.GetAsync<List<ChatMessage>>(CacheKey(threadId), cancellationToken: cancellationToken) ?? [];

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
