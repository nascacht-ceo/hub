using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace nc.Extensions;


public static class DistributedCacheExtensions
{
    public static async Task SetAsync<T>(this IDistributedCache cache,
        string key,
        T value,
        DistributedCacheEntryOptions? options = null,
        JsonSerializerOptions? jsonOptions = null,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, jsonOptions);
        await cache.SetStringAsync(key, json, options ?? new DistributedCacheEntryOptions(), cancellationToken);
    }

    public static async Task<T?> GetAsync<T>(
        this IDistributedCache cache,
        string key,
        JsonSerializerOptions? jsonOptions = null, 
        CancellationToken cancellationToken = default)
    {
        var json = await cache.GetStringAsync(key, cancellationToken);
        return json is null ? default : JsonSerializer.Deserialize<T>(json, jsonOptions);
    }
}
