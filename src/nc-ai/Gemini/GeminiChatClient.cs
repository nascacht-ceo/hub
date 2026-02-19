using Google.Apis.Auth.OAuth2;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace nc.Ai.Gemini;

/// <summary>
/// An <see cref="IChatClient"/> that uses Gemini's explicit context caching to avoid
/// re-sending a large system instruction on every request. Create the cache once via
/// <see cref="CreateCacheAsync"/>, then call <see cref="GetResponseAsync"/> or
/// <see cref="GetStreamingResponseAsync"/> for each document.
/// </summary>
public class GeminiChatClient : IChatClient, IAiContextClient
{
	private readonly Client _client;
	private readonly IAiContextCache _contextCache;
	private readonly string _model;
	private string? _cachedContentName;

	public GeminiChatClient(IAiContextCache contextCache, string model, bool? vertexAI = null, string? apiKey = null, ICredential? credential = null, string? project = null, string? location = null, HttpOptions? httpOptions = null)
	{
		_contextCache = contextCache ?? throw new ArgumentNullException(nameof(contextCache));
		_model = model;
		_client = new Client(vertexAI, apiKey, credential, project, location, httpOptions);
	}

	public GeminiChatClient(IAiContextCache contextCache, Client client, string model)
	{
		_contextCache = contextCache ?? throw new ArgumentNullException(nameof(contextCache));
		_client = client ?? throw new ArgumentNullException(nameof(client));
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
		var config = new CreateCachedContentConfig
		{
			SystemInstruction = new Content
			{
				Parts = [new Part { Text = systemInstruction }]
			},
			Ttl = ttl,
			DisplayName = displayName
		};

		var cachedContent = await _client.Caches.CreateAsync(
			_model, config, cancellationToken);

		_cachedContentName = cachedContent.Name
			?? throw new InvalidOperationException("Gemini did not return a cached content name.");

		return _cachedContentName;
	}

	/// <summary>
	/// Deletes the active cached content resource.
	/// </summary>
	public async Task DeleteCacheAsync(CancellationToken cancellationToken = default)
	{
		if (_cachedContentName is null) return;

		await _client.Caches.DeleteAsync(
			_cachedContentName, cancellationToken: cancellationToken);
		_cachedContentName = null;
	}

	public async Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		var contents = BuildContents(messages);
		var config = BuildConfig(options);

		var response = await _client.Models.GenerateContentAsync(
			_model, contents, config, cancellationToken);

		return ToClientResponse(response);
	}

	public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var contents = BuildContents(messages);
		var config = BuildConfig(options);

		await foreach (var chunk in _client.Models.GenerateContentStreamAsync(
			_model, contents, config, cancellationToken))
		{
			var parts = chunk.Candidates?.FirstOrDefault()?.Content?.Parts;
			if (parts is null) continue;

			foreach (var part in parts)
			{
				if (part.FunctionCall is { } fc)
				{
					yield return new ChatResponseUpdate(ChatRole.Assistant,
					[
						new FunctionCallContent(
							callId: fc.Id ?? fc.Name ?? Guid.NewGuid().ToString(),
							name: fc.Name ?? string.Empty,
							arguments: fc.Args?.ToDictionary(k => k.Key, k => (object?)k.Value))
					]);
				}
				else if (part.Text is { } text)
				{
					yield return new ChatResponseUpdate(ChatRole.Assistant, text);
				}
			}
		}
	}

	public void Dispose() { }

	public object? GetService(System.Type serviceType, object? serviceKey = null)
	{
		if (serviceType == typeof(GeminiChatClient)) return this;
		return null;
	}

	private GenerateContentConfig BuildConfig(ChatOptions? options)
	{
		var config = new GenerateContentConfig { CachedContent = _cachedContentName };

		if (options?.Tools is { Count: > 0 } tools)
		{
			var declarations = tools
				.OfType<AIFunction>()
				.Select(f => new FunctionDeclaration
				{
					Name = f.Name,
					Description = f.Description,
					ParametersJsonSchema = f.JsonSchema
				})
				.ToList();

			if (declarations.Count > 0)
				config.Tools = [new Tool { FunctionDeclarations = declarations }];
		}

		return config;
	}

	private static List<Content> BuildContents(IEnumerable<ChatMessage> messages)
	{
		var contents = new List<Content>();
		var callIdToName = new Dictionary<string, string>();

		foreach (var message in messages)
		{
			var parts = new List<Part>();
			foreach (var content in message.Contents)
			{
				switch (content)
				{
					case TextContent text:
						parts.Add(new Part { Text = text.Text });
						break;
					case FunctionCallContent fc:
						callIdToName[fc.CallId] = fc.Name;
						parts.Add(new Part
						{
							FunctionCall = new FunctionCall
							{
								Id = fc.CallId,
								Name = fc.Name,
								Args = fc.Arguments?.ToDictionary(k => k.Key, k => k.Value ?? (object)string.Empty)
							}
						});
						break;
					case FunctionResultContent result:
						var funcName = callIdToName.TryGetValue(result.CallId, out var n) ? n : result.CallId;
						parts.Add(new Part
						{
							FunctionResponse = new FunctionResponse
							{
								Id = result.CallId,
								Name = funcName,
								Response = new Dictionary<string, object>
								{
									{ "output", result.Result ?? string.Empty }
								}
							}
						});
						break;
					case UriContent uri when uri.Uri is not null:
						parts.Add(new Part
						{
							FileData = new FileData
							{
								MimeType = uri.MediaType,
								FileUri = uri.Uri.ToString()
							}
						});
						break;
					case HostedFileContent hosted:
						parts.Add(new Part
						{
							FileData = new FileData
							{
								MimeType = hosted.MediaType,
								FileUri = hosted.FileId
							}
						});
						break;
					case DataContent data:
						parts.Add(new Part
						{
							InlineData = new Blob
							{
								MimeType = data.MediaType,
								Data = data.Data.ToArray()
							}
						});
						break;
				}
			}

			var role = message.Role == ChatRole.Assistant ? "model" : "user";
			contents.Add(new Content
			{
				Role = role,
				Parts = parts
			});
		}

		return contents;
	}

	private static ChatResponse ToClientResponse(GenerateContentResponse response)
	{
		var candidate = response.Candidates?.FirstOrDefault();
		var messageParts = new List<AIContent>();

		foreach (var part in candidate?.Content?.Parts ?? [])
		{
			if (part.FunctionCall is { } fc)
			{
				messageParts.Add(new FunctionCallContent(
					callId: fc.Id ?? fc.Name ?? Guid.NewGuid().ToString(),
					name: fc.Name ?? string.Empty,
					arguments: fc.Args?.ToDictionary(k => k.Key, k => (object?)k.Value)));
			}
			else if (part.Text is { } text)
			{
				messageParts.Add(new TextContent(text));
			}
		}

		if (messageParts.Count == 0)
			messageParts.Add(new TextContent(string.Empty));

		return new ChatResponse(new ChatMessage(ChatRole.Assistant, messageParts))
		{
			Usage = response.UsageMetadata is { } usage ? new UsageDetails
			{
				InputTokenCount = usage.PromptTokenCount,
				OutputTokenCount = usage.CandidatesTokenCount,
				TotalTokenCount = usage.TotalTokenCount,
				CachedInputTokenCount = usage.CachedContentTokenCount
			} : null
		};
	}
}
