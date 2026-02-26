namespace nc.Ai.Interfaces;

/// <summary>
/// Marker interface indicating that an <see cref="Microsoft.Extensions.AI.IChatClient"/>
/// manages conversation history server-side. When detected by <see cref="nc.Ai.ConversationChatClient"/>,
/// the client-side store is bypassed and the <c>ConversationId</c> in <c>ChatOptions</c>
/// is forwarded directly to the provider.
/// </summary>
public interface INativeConversations;

/// <summary>
/// Singleton marker returned from <c>GetService</c> by providers that support
/// <see cref="INativeConversations"/>. Providers should return this instance rather
/// than <c>this</c>, since <c>GetService&lt;T&gt;</c> casts the result to <typeparamref name="T"/>.
/// </summary>
public sealed class NativeConversationsMarker : INativeConversations
{
	public static readonly NativeConversationsMarker Instance = new();
	private NativeConversationsMarker() { }
}
