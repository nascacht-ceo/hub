using Microsoft.Extensions.AI;

namespace nc.Ai;

public class Instructions: AIContent
{
	/// <summary>
	/// Gets or sets the cache key used to identify the current context in caching operations.
	/// </summary>
	/// <remarks>Use this property to provide a unique identifier for caching data associated with a specific
	/// context. Changing the value may affect cache retrieval and storage behavior.</remarks>
	public required string ContextCacheKey { get; set; }
}
