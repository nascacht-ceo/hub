using Microsoft.Extensions.Options;
using System.IO.Pipelines;
using System.Net.Http.Headers;
using System.Text.Json;

namespace nc.Ai.Gemini;

public class GeminiFileService : IAiFileService<GeminiFileService, string>
{
	private readonly GeminiFileServiceOptions _options;
	private readonly HttpClient _httpClient;
	private readonly HttpClient _geminiClient;

	// Inject HttpClient and the core GeminiClient
	public GeminiFileService(IHttpClientFactory httpClientFactory, IOptions<GeminiFileServiceOptions> options)
	{
		_options = options.Value;
		_httpClient = httpClientFactory.CreateClient(_options.DownloadClientName);

		// 2. Client for the Gemini File API
		_geminiClient = httpClientFactory.CreateClient(_options.UploadClientName);
		_geminiClient.BaseAddress = new Uri(_options.UploadClientUrl);
	}

	public async Task<string> UploadAsync(PipeReader reader, string mediaType, CancellationToken cancellationToken)
	{
		// 1. Prepare the multipart form data content
		using var content = new MultipartFormDataContent("----GeminiUploadBoundary----");

		var metadataObject = new
		{
			file = new
			{
				displayName = "upload.bin",
				// Add purpose here if required by your version/endpoint
				purpose = "file-content" // Or "assistants", depending on use case
			}
		};

		// 2. Add the purpose metadata
		var metadataContent = new StringContent(JsonSerializer.Serialize(metadataObject), System.Text.Encoding.UTF8, "application/json");
		content.Add(metadataContent, name: "metadata");

		// var incoming = new StreamReader(reader.AsStream()).ReadToEnd();
		// 3. Create a custom StreamContent that reads from the PipeReader
		var streamContent = new StreamContent(reader.AsStream(leaveOpen: false));
		streamContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);

		// The API expects the file part to be named "file"
		content.Add(streamContent, name: "file", fileName: "upload.bin");



		// 4. Send the request to the Gemini Files API endpoint
		using var request = new HttpRequestMessage(HttpMethod.Post, _geminiClient.BaseAddress)
		{
			Content = content
		};

		// Add API key for authentication
		request.Headers.Add("x-goog-api-key", _options.ApiKey);

		using var uploadResponse = await _geminiClient.SendAsync(request, cancellationToken);
		var text = new StreamReader(uploadResponse.Content.ReadAsStream()).ReadToEnd();
		uploadResponse.EnsureSuccessStatusCode();

		// 5. Parse the response to get the File ID (resource name)
		using var responseStream = await uploadResponse.Content.ReadAsStreamAsync(cancellationToken);
		var fileResponse = await JsonSerializer.DeserializeAsync<GeminiFileResponse>(responseStream, cancellationToken: cancellationToken);

		if (fileResponse?.Name is null)
		{
			throw new InvalidOperationException("Gemini File API did not return a valid file resource name.");
		}

		return fileResponse.Name;
	}

	public async Task<string> UploadAsync(Stream reader, string mediaType, CancellationToken cancellationToken)
	{
		// 1. Prepare the multipart form data content
		using var content = new MultipartFormDataContent("----GeminiUploadBoundary----");

		var metadataObject = new
		{
			file = new
			{
				displayName = "upload.bin",
				// Add purpose here if required by your version/endpoint
				purpose = "file-content" // Or "assistants", depending on use case
			}
		};

		// 2. Add the purpose metadata
		var metadataContent = new StringContent(JsonSerializer.Serialize(metadataObject), System.Text.Encoding.UTF8, "application/json");
		content.Add(metadataContent, name: "metadata");

		// var incoming = new StreamReader(reader.AsStream()).ReadToEnd();
		// 3. Create a custom StreamContent that reads from the PipeReader
		var streamContent = new StreamContent(reader);
		streamContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);

		// The API expects the file part to be named "file"
		content.Add(streamContent, name: "file", fileName: "upload.bin");



		// 4. Send the request to the Gemini Files API endpoint
		using var request = new HttpRequestMessage(HttpMethod.Post, _geminiClient.BaseAddress)
		{
			Content = content
		};

		// Add API key for authentication
		request.Headers.Add("x-goog-api-key", _options.ApiKey);

		using var uploadResponse = await _geminiClient.SendAsync(request, cancellationToken);
		var text = new StreamReader(uploadResponse.Content.ReadAsStream()).ReadToEnd();
		uploadResponse.EnsureSuccessStatusCode();

		// 5. Parse the response to get the File ID (resource name)
		using var responseStream = await uploadResponse.Content.ReadAsStreamAsync(cancellationToken);
		var fileResponse = await JsonSerializer.DeserializeAsync<GeminiFileResponse>(responseStream, cancellationToken: cancellationToken);

		if (fileResponse?.Name is null)
		{
			throw new InvalidOperationException("Gemini File API did not return a valid file resource name.");
		}

		return fileResponse.Name;
	}

	//public async Task<string> UploadFileFromUriAsync(Uri uri, string mediaType, CancellationToken cancellationToken)
	//{
	//	ArgumentNullException.ThrowIfNull(uri);

	//	// 1. Download the file content from the external URI
	//	// Note: Using a Stream for larger file efficiency
	//	using var externalRequest = new HttpRequestMessage(HttpMethod.Get, uri);
	//	using var externalResponse = await _httpClient.SendAsync(externalRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

	//	externalResponse.EnsureSuccessStatusCode();

	//	// Use the media type passed, or try to infer from the response header
	//	string finalMediaType = externalResponse.Content.Headers.ContentType?.MediaType ?? mediaType;

	//	// Read the stream into a byte array
	//	var fileBytes = await externalResponse.Content.ReadAsByteArrayAsync(cancellationToken);

	//	// 2. Upload to Gemini using Multipart Form Data
	//	using var content = new MultipartFormDataContent();

	//	// Add the file as binary content
	//	var fileContent = new ByteArrayContent(fileBytes);
	//	fileContent.Headers.ContentType = new MediaTypeHeaderValue(finalMediaType);

	//	// The API expects the file part to be named "file" (REST convention)
	//	content.Add(fileContent, name: "file", fileName: uri.Segments.Last());

	//	// Add the purpose metadata (required for file uploads)
	//	// Note: The specific metadata format may vary by API version/client library. 
	//	// We'll use a common pattern for file uploads.
	//	var metadataContent = new StringContent(
	//		JsonSerializer.Serialize(new { purpose = "file-content" }),
	//		System.Text.Encoding.UTF8,
	//		"application/json"
	//	);
	//	content.Add(metadataContent, name: "metadata");

	//	// 3. Send the request to the Gemini Files API endpoint
	//	using var request = new HttpRequestMessage(HttpMethod.Post, _geminiClient.BaseAddress)
	//	{
	//		Content = content
	//	};

	//	// Add API key for authentication
	//	request.Headers.Add("x-goog-api-key", _options.ApiKey);

	//	using var uploadResponse = await _geminiClient.SendAsync(request, cancellationToken);
	//	uploadResponse.EnsureSuccessStatusCode();

	//	// 4. Parse the response to get the File ID (resource name)
	//	using var responseStream = await uploadResponse.Content.ReadAsStreamAsync(cancellationToken);

	//	// Define a minimal class to parse the response
	//	var fileResponse = await JsonSerializer.DeserializeAsync<GeminiFileResponse>(responseStream, cancellationToken: cancellationToken);

	//	if (fileResponse?.Name is null)
	//	{
	//		throw new InvalidOperationException("Gemini File API did not return a valid file resource name.");
	//	}

	//	// The file ID is the resource name (e.g., "files/abc-123")
	//	return fileResponse.Name;
	//}

	private class GeminiFileResponse
	{
		public string? Name { get; set; } // The file ID, e.g., "files/abc-123"
		public string? DisplayName { get; set; }
		// Add other properties if needed
	}

	/// <summary>
	/// Wraps a PipeReader as a readable Stream.
	/// </summary>
	//private static Stream PipeReaderAsStream(PipeReader reader, CancellationToken cancellationToken)
	//{
	//	return PipeReader.CreateStream(reader, leaveOpen: false, cancellationToken: cancellationToken);
	//}
}

