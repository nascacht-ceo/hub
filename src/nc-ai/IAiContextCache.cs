using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Text;

namespace nc.Ai;

public interface IAiContextCache
{
	public Task<AIContent> GetContextAsync(string key);

	public Task SetContextAsync(string key, AIContent content, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default);
}
