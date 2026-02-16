using Microsoft.Extensions.AI;
using System.Collections.Concurrent;

namespace nc.Ai.Caching;

/// <summary>
/// A cache strategy that stores prompts in-process and replaces
/// <see cref="CachedPromptReference"/> with the original text at call time.
/// Suitable for providers like Anthropic that offer implicit prefix-based
/// caching, or for any provider without an explicit caching API.
/// </summary>
public class PassthroughCacheStrategy : ICacheStrategy
{
	private readonly ConcurrentDictionary<string, string> _cache = new();

	public Task<string> CreateCacheAsync(
		string systemPrompt,
		TimeSpan ttl,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(systemPrompt);

		var cacheId = Guid.NewGuid().ToString("N");
		_cache.TryAdd(cacheId, systemPrompt);
		return Task.FromResult(cacheId);
	}

	public Task DeleteCacheAsync(
		string cacheId,
		CancellationToken cancellationToken = default)
	{
		_cache.TryRemove(cacheId, out _);
		return Task.CompletedTask;
	}

	public IEnumerable<ChatMessage> TransformMessages(
		IEnumerable<ChatMessage> messages)
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
					if (!_cache.TryGetValue(cached.CacheId, out var prompt))
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
