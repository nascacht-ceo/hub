using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace nc.Ai.Gemini;

/// <summary>
/// A <see cref="DelegatingChatClient"/> that transforms <see cref="UriContent"/> (http/https)
/// into <see cref="HostedFileContent"/> by downloading the file and uploading it to the
/// Gemini Files API via <see cref="Client.Files"/>. This enables Gemini to process files
/// from URLs it cannot fetch directly (e.g. S3 presigned URLs, authenticated endpoints).
/// </summary>
public class GeminiFileUploader : DelegatingChatClient
{
	private readonly Client _genAiClient;
	private readonly HttpClient _httpClient;

	public GeminiFileUploader(IChatClient inner, Client genAiClient, HttpClient? httpClient = null)
		: base(inner)
	{
		_genAiClient = genAiClient ?? throw new ArgumentNullException(nameof(genAiClient));
		_httpClient = httpClient ?? new HttpClient();
	}

	public override async Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages, ChatOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		await UploadUriContentsAsync(messages, cancellationToken);
		return await base.GetResponseAsync(messages, options, cancellationToken);
	}

	public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> messages, ChatOptions? options = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		await UploadUriContentsAsync(messages, cancellationToken);
		await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
			yield return update;
	}

	private async Task UploadUriContentsAsync(
		IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
	{
		foreach (var message in messages)
		{
			for (int i = 0; i < message.Contents.Count; i++)
			{
				if (message.Contents[i] is UriContent uri && uri.Uri?.Scheme is "http" or "https")
				{
					try
					{
						var bytes = await _httpClient.GetByteArrayAsync(uri.Uri, cancellationToken);
						var fileName = Path.GetFileName(uri.Uri.LocalPath) is { Length: > 0 } name
							? name : "upload.bin";

						var file = await _genAiClient.Files.UploadAsync(
							bytes, fileName,
							new UploadFileConfig { MimeType = uri.MediaType },
							cancellationToken);

						file = await WaitForActiveAsync(file, cancellationToken);

						message.Contents[i] = new HostedFileContent(file.Uri ?? file.Name!)
						{
							MediaType = uri.MediaType
						};
					}
					catch (HttpRequestException ex)
					{
						throw new ArgumentException(nameof(uri), "Failed to download content from URI: " + uri.Uri, ex);
					}
					catch
					{
						throw;
					}

				}
			}
		}
	}

	private async Task<Google.GenAI.Types.File> WaitForActiveAsync(
		Google.GenAI.Types.File file, CancellationToken cancellationToken)
	{
		while (file.State == FileState.Processing)
		{
			await Task.Delay(1000, cancellationToken);
			file = await _genAiClient.Files.GetAsync(file.Name!, cancellationToken: cancellationToken);
		}

		if (file.State == FileState.Failed)
			throw new InvalidOperationException(
				$"Gemini file processing failed for '{file.Name}': {file.Error}");

		return file;
	}
}
