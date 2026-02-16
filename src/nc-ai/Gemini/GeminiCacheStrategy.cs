using Microsoft.Extensions.AI;
using nc.Ai.Caching;

namespace nc.Ai.Gemini;

/// <summary>
/// Cache strategy for Google Gemini. Delegates to <see cref="GeminiCachedChatClient"/>
/// for server-side explicit context caching (75-90% cost reduction on cached tokens).
/// Falls back to <see cref="PassthroughCacheStrategy"/> when the prompt is below
/// Gemini's 2,048-token minimum (estimated via word count).
/// </summary>
public class GeminiCacheStrategy : ICacheStrategy
{
	/// <summary>
	/// Gemini requires at least 2,048 tokens for cached content.
	/// Words are a conservative proxy — 2,048 words always exceeds 2,048 tokens.
	/// </summary>
	internal const int MinWordCount = 2048;

	private readonly GeminiCachedChatClient _cachedClient;
	private readonly PassthroughCacheStrategy _fallback = new();
	private bool _usingFallback;

	/// <param name="cachedClient">
	/// The <see cref="GeminiCachedChatClient"/> instance that is also the
	/// inner client in the pipeline. Must be the same instance that
	/// <see cref="CachedChatClient"/> delegates to.
	/// </param>
	public GeminiCacheStrategy(GeminiCachedChatClient cachedClient)
	{
		ArgumentNullException.ThrowIfNull(cachedClient);
		_cachedClient = cachedClient;
	}

	public async Task<string> CreateCacheAsync(
		string systemPrompt,
		TimeSpan ttl,
		CancellationToken cancellationToken = default)
	{
		var wordCount = systemPrompt.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

		if (wordCount < MinWordCount)
		{
			_usingFallback = true;
			return await _fallback.CreateCacheAsync(systemPrompt, ttl, cancellationToken);
		}

		var ttlSeconds = $"{(int)ttl.TotalSeconds}s";
		return await _cachedClient.CreateCacheAsync(
			systemPrompt, ttl: ttlSeconds, cancellationToken: cancellationToken);
	}

	public async Task DeleteCacheAsync(
		string cacheId,
		CancellationToken cancellationToken = default)
	{
		if (_usingFallback)
			await _fallback.DeleteCacheAsync(cacheId, cancellationToken);
		else
			await _cachedClient.DeleteCacheAsync(cancellationToken);
	}

	public IEnumerable<ChatMessage> TransformMessages(
		IEnumerable<ChatMessage> messages)
	{
		return _usingFallback
			? _fallback.TransformMessages(messages)
			: StripCachedReferences(messages);
	}

	private static IEnumerable<ChatMessage> StripCachedReferences(
		IEnumerable<ChatMessage> messages)
	{
		foreach (var message in messages)
		{
			if (!message.Contents.Any(c => c is CachedPromptReference))
			{
				yield return message;
				continue;
			}

			var filtered = message.Contents
				.Where(c => c is not CachedPromptReference)
				.ToList();

			// Drop messages that only contained a CachedPromptReference
			// to avoid sending an empty contents array.
			if (filtered.Count > 0)
				yield return new ChatMessage(message.Role, filtered);
		}
	}
}
