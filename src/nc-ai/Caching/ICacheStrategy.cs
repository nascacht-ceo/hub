using Microsoft.Extensions.AI;

namespace nc.Ai.Caching;

/// <summary>
/// Defines how prompt caching is handled for a specific AI provider.
/// Implementations manage cache lifecycle and transform messages to
/// consume cached content.
/// </summary>
public interface ICacheStrategy
{
	/// <summary>
	/// Creates a cached prompt with the provider.
	/// </summary>
	/// <param name="systemPrompt">The large system instruction text to cache.</param>
	/// <param name="ttl">How long the cache should live.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>An opaque cache identifier to pass into <see cref="CachedPromptReference"/>.</returns>
	Task<string> CreateCacheAsync(
		string systemPrompt,
		TimeSpan ttl,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes a previously created cache.
	/// </summary>
	/// <param name="cacheId">The identifier returned by <see cref="CreateCacheAsync"/>.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task DeleteCacheAsync(string cacheId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Transforms messages before they are sent to the inner <see cref="IChatClient"/>.
	/// Implementations replace or strip <see cref="CachedPromptReference"/> items.
	/// </summary>
	/// <param name="messages">The original messages from the consumer.</param>
	/// <returns>Transformed messages ready for the provider.</returns>
	IEnumerable<ChatMessage> TransformMessages(IEnumerable<ChatMessage> messages);
}
