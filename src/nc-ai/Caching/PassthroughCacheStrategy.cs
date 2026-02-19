using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using nc.Extensions;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace nc.Ai.Caching;

/// <summary>
/// A cache strategy that stores prompts in-process and replaces
/// <see cref="CachedPromptReference"/> with the original text at call time.
/// Suitable for providers like Anthropic that offer implicit prefix-based
/// caching, or for any provider without an explicit caching API.
/// </summary>
public class PassthroughCacheStrategy : ICacheStrategy
{
	private readonly IDistributedCache _cache;

	public PassthroughCacheStrategy(IDistributedCache? cache = null)
	{
		_cache = cache ?? new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions() { }));
	}


	public async Task<string> CreateCacheAsync(
		string systemPrompt,
		TimeSpan ttl,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(systemPrompt);

		var cacheId = Guid.NewGuid().ToString("N");
		await _cache.SetAsync(cacheId, systemPrompt);
		return cacheId;
	}

	public Task DeleteCacheAsync(
		string cacheId,
		CancellationToken cancellationToken = default)
	{
		return _cache.RemoveAsync(cacheId);
	}

	public async IAsyncEnumerable<ChatMessage> TransformMessages(
		IEnumerable<ChatMessage> messages,
		[EnumeratorCancellation]CancellationToken cancellationToken = default)
	{
		foreach (var message in messages)
		{
			if (!message.Contents.Any(c => c is CachedPromptReference))
			{
				yield return message;
				continue;
			}

			var expanded = new List<AIContent>();
			foreach (var content in message.Contents)
			{
				if (content is CachedPromptReference cached)
				{
					var prompt = await _cache.GetAsync<string>(cached.CacheId);
					if (string.IsNullOrEmpty(prompt))
						throw new InvalidOperationException(
							$"No cached prompt found for cache ID '{cached.CacheId}'. " +
							"Was it deleted or never created?");
					expanded.Add(new TextContent(prompt));
				}
				else
				{
					expanded.Add(content);
				}
			}

			yield return new ChatMessage(message.Role, expanded);
		}
	}
}
