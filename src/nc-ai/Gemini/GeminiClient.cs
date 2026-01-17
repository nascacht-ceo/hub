using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace nc.Ai.Gemini;

public class GeminiClient : DelegatingChatClient
{
	private readonly IChatClient _innerClient;
	private readonly GeminiFileService _fileService;

	public GeminiClient(IChatClient innerClient, GeminiFileService fileService) : base(innerClient)
	{
		_innerClient = innerClient;
		_fileService = fileService;
	}
	public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation]CancellationToken cancellationToken = default)
	{
		var transformedMessages = await TransformMessagesAsync(messages, cancellationToken).ToListAsync();
		await foreach (var response in base.GetStreamingResponseAsync(transformedMessages, options, cancellationToken))
			yield return response;
	}

	public override async Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		var transformedMessages = await TransformMessagesAsync(messages, cancellationToken).ToListAsync();
		return await base.GetResponseAsync(transformedMessages, options, cancellationToken);
	}

	private async IAsyncEnumerable<ChatMessage> TransformMessagesAsync(
		IEnumerable<ChatMessage> messages,
		[EnumeratorCancellation]CancellationToken cancellationToken = default)
	{
		var newMessages = new List<ChatMessage>();

		foreach (var message in messages)
		{
			var newContents = new List<AIContent>();
			bool wasTransformed = false;

			foreach (var content in message.Contents)
			{
				if (content is UriContent uriContent)
				{
					// Found a UriContent: Upload the file and replace it with a HostedFileContent.

					// You might add logic here to only handle certain schemas (e.g., "https://").
					if (uriContent.Uri != null)
					{
						wasTransformed = true;

						// 1. Use the dedicated service to upload the content.
						string fileId = await _fileService.UploadUriAsync(
							uriContent.Uri,
							uriContent.MediaType,
							cancellationToken
						);

						// 2. Replace UriContent with HostedFileContent
						newContents.Add(new HostedFileContent(fileId: fileId));
					}
					else
					{
						// Keep the original content if URI is missing
						newContents.Add(uriContent);
					}
				}
				else
				{
					// Keep all other content types (TextContent, FunctionResultContent, etc.)
					newContents.Add(content);
				}

			}

			// Create a new message only if content was transformed, otherwise reuse the original message object
			if (wasTransformed)
			{
				// newMessages.Add(new ChatMessage(message.Role, newContents, message.Name));
				yield return new ChatMessage(message.Role, newContents);
			}
			else
			{
				yield return message;
			}
		}
	}
}
