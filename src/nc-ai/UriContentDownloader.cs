using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace nc.Ai;

/// <summary>
/// A <see cref="DelegatingChatClient"/> that resolves HTTP/HTTPS <c>UriContent</c> items
/// in the message list into inline <c>DataContent</c> before forwarding to the inner client.
/// Providers that do not accept remote URIs (e.g. Anthropic, Azure AI Foundry) use this
/// to transparently download and inline the referenced content.
/// </summary>
public class UriContentDownloader : DelegatingChatClient
{
	/// <summary>Initializes the downloader with the inner client to delegate to.</summary>
	/// <param name="inner">The underlying chat client.</param>
	public UriContentDownloader(IChatClient inner) : base(inner) { }

	private static async Task ResolveUriContentAsync(IEnumerable<ChatMessage> messages)
	{
		using var httpClient = new HttpClient();
		foreach (var message in messages)
		{
			for (int i = 0; i < message.Contents.Count; i++)
			{
				if (message.Contents[i] is UriContent uri && uri.Uri.Scheme is "http" or "https")
				{
					var bytes = await httpClient.GetByteArrayAsync(uri.Uri);
					message.Contents[i] = new DataContent(bytes, uri.MediaType);
				}
			}
		}
	}

	//public override Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
	//{
	//	return base.GetResponseAsync(messages, options, cancellationToken);
	//}
	/// <inheritdoc/>
	public override async Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
	{
		await ResolveUriContentAsync(messages);
		return await base.GetResponseAsync(messages, options, ct);
	}

	/// <inheritdoc/>
	public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> messages, ChatOptions? options = null,
		[EnumeratorCancellation] CancellationToken ct = default)
	{
		await ResolveUriContentAsync(messages);
		await foreach (var update in base.GetStreamingResponseAsync(messages, options, ct))
			yield return update;
	}
}