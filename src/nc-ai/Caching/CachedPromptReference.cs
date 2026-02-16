using Microsoft.Extensions.AI;

namespace nc.Ai.Caching;

/// <summary>
/// A content placeholder referencing a previously cached prompt.
/// Place this in a <see cref="ChatMessage.Contents"/> list. The
/// <see cref="CachedChatClient"/> will transform or strip it before
/// the message reaches the underlying provider.
/// </summary>
public class CachedPromptReference : AIContent
{
	/// <summary>
	/// The opaque identifier returned by <see cref="ICacheStrategy.CreateCacheAsync"/>.
	/// </summary>
	public string CacheId { get; }

	public CachedPromptReference(string cacheId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(cacheId);
		CacheId = cacheId;
	}
}
