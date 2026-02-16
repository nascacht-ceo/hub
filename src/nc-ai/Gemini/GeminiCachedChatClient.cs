using Microsoft.Extensions.AI;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace nc.Ai.Gemini;

/// <summary>
/// An <see cref="IChatClient"/> that uses Gemini's explicit context caching to avoid
/// re-sending a large system instruction on every request. Create the cache once via
/// <see cref="CreateCacheAsync"/>, then call <see cref="GetResponseAsync"/> or
/// <see cref="GetStreamingResponseAsync"/> for each document.
/// </summary>
public class GeminiCachedChatClient : IChatClient
{
	private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta";

	private readonly HttpClient _httpClient;
	private readonly string _apiKey;
	private readonly string _model;
	private readonly JsonSerializerOptions _jsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	private string? _cachedContentName;

	public GeminiCachedChatClient(HttpClient httpClient, string apiKey, string model)
	{
		_httpClient = httpClient;
		_apiKey = apiKey;
		_model = model;
	}

	/// <summary>
	/// The resource name of the active cached content (e.g. "cachedContents/abc123"),
	/// or null if no cache has been created yet.
	/// </summary>
	public string? CachedContentName => _cachedContentName;

	/// <summary>
	/// Creates a cached content resource from a system instruction.
	/// Subsequent calls to <see cref="GetResponseAsync"/> will reference this cache
	/// instead of re-sending the instruction.
	/// </summary>
	/// <param name="systemInstruction">The large prompt to cache.</param>
	/// <param name="ttl">Cache time-to-live (e.g. "3600s"). Defaults to 1 hour.</param>
	/// <param name="displayName">Optional display name for the cache entry.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The cached content resource name.</returns>
	public async Task<string> CreateCacheAsync(
		string systemInstruction,
		string ttl = "3600s",
		string? displayName = null,
		CancellationToken cancellationToken = default)
	{
		var request = new CacheCreateRequest
		{
			Model = $"models/{_model}",
			SystemInstruction = new GeminiContent
			{
				Parts = [new GeminiPart { Text = systemInstruction }]
			},
			Ttl = ttl,
			DisplayName = displayName
		};

		var response = await PostAsync<CacheCreateRequest, CacheCreateResponse>(
			$"{BaseUrl}/cachedContents", request, cancellationToken);

		_cachedContentName = response.Name
			?? throw new InvalidOperationException("Gemini did not return a cached content name.");

		return _cachedContentName;
	}

	/// <summary>
	/// Deletes the active cached content resource.
	/// </summary>
	public async Task DeleteCacheAsync(CancellationToken cancellationToken = default)
	{
		if (_cachedContentName is null) return;

		using var request = new HttpRequestMessage(HttpMethod.Delete,
			$"{BaseUrl}/{_cachedContentName}?key={_apiKey}");
		using var response = await _httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();
		_cachedContentName = null;
	}

	public async Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		var generateResponse = await GenerateContentAsync(messages, cancellationToken);
		return ToClientResponse(generateResponse);
	}

	public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		// Gemini's streamGenerateContent returns newline-delimited JSON chunks.
		var url = $"{BaseUrl}/models/{_model}:streamGenerateContent?alt=sse&key={_apiKey}";
		var body = BuildRequestBody(messages);
		using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
		{
			Content = new StringContent(JsonSerializer.Serialize(body, _jsonOptions), Encoding.UTF8, "application/json")
		};
		using var httpResponse = await _httpClient.SendAsync(httpRequest,
			HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		httpResponse.EnsureSuccessStatusCode();

		using var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
		using var reader = new StreamReader(stream);

		string? line;
		while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
		{
			if (!line.StartsWith("data: ")) continue;

			var json = line["data: ".Length..];
			var chunk = JsonSerializer.Deserialize<GenerateContentResponse>(json, _jsonOptions);
			var text = chunk?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
			if (text is not null)
			{
				yield return new ChatResponseUpdate(ChatRole.Assistant, text);
			}
		}
	}

	public void Dispose() { }

	public object? GetService(Type serviceType, object? serviceKey = null)
	{
		if (serviceType == typeof(GeminiCachedChatClient)) return this;
		return null;
	}

	private async Task<GenerateContentResponse> GenerateContentAsync(
		IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
	{
		var url = $"{BaseUrl}/models/{_model}:generateContent?key={_apiKey}";
		var body = BuildRequestBody(messages);
		return await PostAsync<GenerateContentRequest, GenerateContentResponse>(
			url, body, cancellationToken);
	}

	private GenerateContentRequest BuildRequestBody(IEnumerable<ChatMessage> messages)
	{
		var contents = new List<GeminiContent>();
		foreach (var message in messages)
		{
			var parts = new List<GeminiPart>();
			foreach (var content in message.Contents)
			{
				switch (content)
				{
					case TextContent text:
						parts.Add(new GeminiPart { Text = text.Text });
						break;
					case UriContent uri when uri.Uri is not null:
						parts.Add(new GeminiPart
						{
							FileData = new GeminiFileData
							{
								MimeType = uri.MediaType,
								FileUri = uri.Uri.ToString()
							}
						});
						break;
					case DataContent data:
						parts.Add(new GeminiPart
						{
							InlineData = new GeminiInlineData
							{
								MimeType = data.MediaType,
								Data = Convert.ToBase64String(data.Data.ToArray())
							}
						});
						break;
				}
			}

			contents.Add(new GeminiContent
			{
				Role = message.Role == ChatRole.User ? "user" : "model",
				Parts = parts
			});
		}

		return new GenerateContentRequest
		{
			Contents = contents,
			CachedContent = _cachedContentName
		};
	}

	private async Task<TResponse> PostAsync<TRequest, TResponse>(
		string url, TRequest body, CancellationToken cancellationToken)
	{
		var fullUrl = url.Contains('?') ? $"{url}&key={_apiKey}" : $"{url}?key={_apiKey}";
		var json = JsonSerializer.Serialize(body, _jsonOptions);
		using var request = new HttpRequestMessage(HttpMethod.Post, fullUrl)
		{
			Content = new StringContent(json, Encoding.UTF8, "application/json")
		};

		using var response = await _httpClient.SendAsync(request, cancellationToken);
		var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

		if (!response.IsSuccessStatusCode)
			throw new HttpRequestException($"Gemini API error ({response.StatusCode}): {responseBody}");

		return JsonSerializer.Deserialize<TResponse>(responseBody, _jsonOptions)
			?? throw new InvalidOperationException($"Failed to deserialize Gemini response: {responseBody}");
	}

	private static ChatResponse ToClientResponse(GenerateContentResponse response)
	{
		var candidate = response.Candidates?.FirstOrDefault();
		var text = candidate?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;

		return new ChatResponse(new ChatMessage(ChatRole.Assistant, text))
		{
			Usage = response.UsageMetadata is { } usage ? new UsageDetails
			{
				InputTokenCount = usage.PromptTokenCount,
				OutputTokenCount = usage.CandidatesTokenCount,
				TotalTokenCount = usage.TotalTokenCount,
				AdditionalCounts = usage.CachedContentTokenCount > 0
					? new AdditionalPropertiesDictionary<long> { ["cachedContentTokenCount"] = usage.CachedContentTokenCount }
					: null
			} : null
		};
	}

	#region Gemini API DTOs

	private class CacheCreateRequest
	{
		public string? Model { get; set; }
		public GeminiContent? SystemInstruction { get; set; }
		public List<GeminiContent>? Contents { get; set; }
		public string? Ttl { get; set; }
		public string? DisplayName { get; set; }
	}

	private class CacheCreateResponse
	{
		public string? Name { get; set; }
		public string? Model { get; set; }
		public CacheUsageMetadata? UsageMetadata { get; set; }
	}

	private class CacheUsageMetadata
	{
		public int TotalTokenCount { get; set; }
	}

	private class GenerateContentRequest
	{
		public List<GeminiContent>? Contents { get; set; }
		public string? CachedContent { get; set; }
	}

	private class GenerateContentResponse
	{
		public List<GeminiCandidate>? Candidates { get; set; }
		public GeminiUsageMetadata? UsageMetadata { get; set; }
	}

	private class GeminiCandidate
	{
		public GeminiContent? Content { get; set; }
		public string? FinishReason { get; set; }
	}

	private class GeminiContent
	{
		public string? Role { get; set; }
		public List<GeminiPart>? Parts { get; set; }
	}

	private class GeminiPart
	{
		public string? Text { get; set; }
		public GeminiFileData? FileData { get; set; }
		public GeminiInlineData? InlineData { get; set; }
	}

	private class GeminiFileData
	{
		public string? MimeType { get; set; }
		public string? FileUri { get; set; }
	}

	private class GeminiInlineData
	{
		public string? MimeType { get; set; }
		public string? Data { get; set; }
	}

	private class GeminiUsageMetadata
	{
		public int PromptTokenCount { get; set; }
		public int CandidatesTokenCount { get; set; }
		public int TotalTokenCount { get; set; }
		public int CachedContentTokenCount { get; set; }
	}

	#endregion
}
