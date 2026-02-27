using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Text;

namespace nc.Ai;

/// <summary>
/// Cache for <see cref="AIContent"/> objects, typically used to store server-side
/// cached content handles returned by providers such as Gemini.
/// </summary>
public interface IAiContextCache
{
	/// <summary>Retrieves the cached content for the given key.</summary>
	/// <param name="key">The cache key.</param>
	/// <exception cref="ArgumentNullException">No entry exists for <paramref name="key"/>.</exception>
	public Task<AIContent> GetContextAsync(string key);

	/// <summary>Stores or replaces cached content under the given key.</summary>
	/// <param name="key">The cache key.</param>
	/// <param name="content">The content to cache.</param>
	/// <param name="options">Override the default expiry; falls back to <see cref="AiContextCacheOptions.EntryOptions"/>.</param>
	/// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
	public Task SetContextAsync(string key, AIContent content, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default);
}
