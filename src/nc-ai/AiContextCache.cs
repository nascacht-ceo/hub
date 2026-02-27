using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nc.Extensions;

namespace nc.Ai;

/// <summary>
/// <see cref="IDistributedCache"/>-backed implementation of <see cref="IAiContextCache"/>.
/// Falls back to an in-memory cache when no <see cref="IDistributedCache"/> is registered.
/// </summary>
public class AiContextCache : IAiContextCache
{
	private readonly IDistributedCache _cache;
	private AiContextCacheOptions _options;
	private readonly ILogger<AiContextCache>? _logger;

	/// <summary>
	/// Initializes the cache, subscribing to option changes via <paramref name="options"/>.
	/// </summary>
	/// <param name="options">Live cache configuration.</param>
	/// <param name="cache">The distributed cache backend; falls back to in-memory when <c>null</c>.</param>
	/// <param name="logger">Optional logger for diagnostic output.</param>
	public AiContextCache(IOptionsMonitor<AiContextCacheOptions> options, IDistributedCache cache, ILogger<AiContextCache>? logger = null)
	{
		_options = options.CurrentValue;
		options.OnChange(o => _options = o);
		_cache = cache ?? new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
		_logger = logger;
	}

	/// <inheritdoc/>
	public async Task<AIContent> GetContextAsync(string key)
	{
		return await _cache.GetAsync<AIContent>(key) ?? throw new ArgumentNullException(key, "No AiContext exists with this key");
	}

	/// <inheritdoc/>
	public Task SetContextAsync(string key, AIContent content, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
	{
		return _cache.SetAsync(key, content, options ?? _options.EntryOptions, null, cancellationToken);
	}
}

/// <summary>
/// Configuration options for <see cref="AiContextCache"/>.
/// </summary>
public class AiContextCacheOptions
{
	/// <summary>Gets or sets the default absolute expiry for cached entries, in minutes. Defaults to 5.</summary>
	public int CacheDurationMinutes { get; set; } = 5;

	private DistributedCacheEntryOptions? _entryOptions;

	/// <summary>Gets the <see cref="DistributedCacheEntryOptions"/> derived from <see cref="CacheDurationMinutes"/>.</summary>
	public DistributedCacheEntryOptions EntryOptions =>
		_entryOptions ??= new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheDurationMinutes) };
}