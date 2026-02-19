using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nc.Extensions;

namespace nc.Ai;

public class AiContextCache : IAiContextCache
{
	private readonly IDistributedCache _cache;
	private AiContextCacheOptions _options;
	private readonly ILogger<AiContextCache>? _logger;

	public AiContextCache(IOptionsMonitor<AiContextCacheOptions> options, IDistributedCache cache, ILogger<AiContextCache>? logger = null)
	{
		_options = options.CurrentValue;
		options.OnChange(o => _options = o);
		_cache = cache ?? new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
		_logger = logger;
	}
	public async Task<AIContent> GetContextAsync(string key)
	{
		return await _cache.GetAsync<AIContent>(key) ?? throw new ArgumentNullException(key, "No AiContext exists with this key");
	}

	public Task SetContextAsync(string key, AIContent content, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
	{
		return _cache.SetAsync(key, content, options ?? _options.EntryOptions, null, cancellationToken);
	}
}

public class AiContextCacheOptions
{
	public int CacheDurationMinutes { get; set; } = 5;

	private DistributedCacheEntryOptions? _entryOptions;
	public DistributedCacheEntryOptions EntryOptions =>
		_entryOptions ??= new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheDurationMinutes) };
}