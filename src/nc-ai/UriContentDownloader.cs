using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace nc.Ai;

public class UriContentDownloader : DelegatingChatClient
{
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
	public override async Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
	{
		await ResolveUriContentAsync(messages);
		return await base.GetResponseAsync(messages, options, ct);
	}

	public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> messages, ChatOptions? options = null,
		[EnumeratorCancellation] CancellationToken ct = default)
	{
		await ResolveUriContentAsync(messages);
		await foreach (var update in base.GetStreamingResponseAsync(messages, options, ct))
			yield return update;
	}
}