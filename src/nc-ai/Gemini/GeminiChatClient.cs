using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace nc.Ai.Gemini;

/// <summary>
/// An <see cref="IChatClient"/> implementation for Google Gemini.
/// When <see cref="ChatOptions.Instructions"/> is set, instructions are
/// transparently cached via <c>Client.Caches</c> when they meet Gemini's
/// minimum token threshold, reducing cost and latency on subsequent calls
/// with the same instruction set. Short instructions are passed inline as
/// <c>SystemInstruction</c>.
/// </summary>
public class GeminiChatClient : IChatClient
{
	internal const int MinInstructionWords = 2048;

	private readonly Client _client;
	private readonly string _model;
	private readonly TimeSpan _cacheTtl;
	private readonly IDistributedCache _instructionCache;

	/// <summary>
	/// Initializes the client, constructing an internal <c>Google.GenAI.Client</c> from
	/// the agent configuration.
	/// </summary>
	/// <param name="options">Agent configuration including model, credentials, and cache TTL.</param>
	/// <param name="cache">Optional distributed cache for instruction cache-name lookup; defaults to in-memory.</param>
	public GeminiChatClient(GeminiAgent options, IDistributedCache? cache = null)
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentException.ThrowIfNullOrEmpty(options.Model, nameof(options.Model));
		_model = options.Model;
		_cacheTtl = options.CacheTtl;
		_instructionCache = cache ?? new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
		var httpOptions = options.HttpOptions
			?? (options.Timeout is { } t ? new HttpOptions { Timeout = (int)t.TotalMilliseconds } : null);
		try
		{
			_client = new Client(options.VertexAI, options.ApiKey, options.Credential, options.Project, options.Location, httpOptions);
		}
		catch(Exception ex)
		{
			throw new InvalidOperationException($"Failed to create Gemini client. Please check your configuration and credentials. VertexAI: {options.VertexAI}; ApiKey {options.ApiKey}; Project: {options.Project}; Location: {options.Location}", ex);
		}
	}

	/// <summary>
	/// Initializes the client with an externally provided <c>Google.GenAI.Client</c> (useful for testing).
	/// </summary>
	/// <param name="client">A pre-configured Google GenAI client.</param>
	/// <param name="options">Agent configuration for model name and cache TTL.</param>
	/// <param name="cache">Optional distributed cache for instruction cache-name lookup; defaults to in-memory.</param>
	public GeminiChatClient(Client client, GeminiAgent options, IDistributedCache? cache = null)
	{
		ArgumentNullException.ThrowIfNull(options);
		_client = client ?? throw new ArgumentNullException(nameof(client));
		_model = options.Model;
		_cacheTtl = options.CacheTtl;
		_instructionCache = cache ?? new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
	}

	/// <inheritdoc/>
	public async Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		var contents = BuildContents(messages);
		var config = await BuildConfigAsync(options, cancellationToken);

		var response = await _client.Models.GenerateContentAsync(
			_model, contents, config, cancellationToken);

		return ToClientResponse(response);
	}

	/// <inheritdoc/>
	public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var contents = BuildContents(messages);
		var config = await BuildConfigAsync(options, cancellationToken);
		var hasFunctionResults = contents.Any(c => c.Parts?.Any(p => p.FunctionResponse != null) == true);

		var yieldedAny = false;
		await foreach (var chunk in _client.Models.GenerateContentStreamAsync(
			_model, contents, config, cancellationToken))
		{
			var parts = chunk.Candidates?.FirstOrDefault()?.Content?.Parts;
			if (parts is null) continue;

			foreach (var part in parts)
			{
				if (part.FunctionCall is { } fc)
				{
					yieldedAny = true;
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
					yieldedAny = true;
					yield return new ChatResponseUpdate(ChatRole.Assistant, text);
				}
			}
		}

		// Gemini 2.5 Pro's thinking mode can consume its streaming budget on internal reasoning
		// after function results, emitting no visible output. Fall back to a non-streaming call
		// so the model can still emit further function calls or a final text response.
		if (!yieldedAny && hasFunctionResults)
		{
			var response = await _client.Models.GenerateContentAsync(_model, contents, config, cancellationToken);
			foreach (var part in response.Candidates?.FirstOrDefault()?.Content?.Parts ?? [])
			{
				if (part.FunctionCall is { } fc)
					yield return new ChatResponseUpdate(ChatRole.Assistant,
					[
						new FunctionCallContent(
							callId: fc.Id ?? fc.Name ?? Guid.NewGuid().ToString(),
							name: fc.Name ?? string.Empty,
							arguments: fc.Args?.ToDictionary(k => k.Key, k => (object?)k.Value))
					]);
				else if (part.Text is { } text)
					yield return new ChatResponseUpdate(ChatRole.Assistant, text);
			}
		}
	}

	public void Dispose() { }

	public object? GetService(System.Type serviceType, object? serviceKey = null)
	{
		if (serviceType == typeof(GeminiChatClient)) return this;
		return null;
	}

	private async Task<GenerateContentConfig> BuildConfigAsync(ChatOptions? options, CancellationToken cancellationToken)
	{
		var config = new GenerateContentConfig();

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

		if (options?.Instructions is { Length: > 0 } instructions)
		{
			var wordCount = instructions.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
			if (wordCount >= MinInstructionWords)
				config.CachedContent = await GetOrCreateCacheNameAsync(instructions, cancellationToken);
			else
				config.SystemInstruction = new Content { Parts = [new Part { Text = instructions }] };
		}

		return config;
	}

	private async Task<string> GetOrCreateCacheNameAsync(string instructions, CancellationToken cancellationToken)
	{
		var hash = ComputeCacheKey(instructions);
		var existing = await _instructionCache.GetStringAsync(hash, cancellationToken);
		if (existing is not null)
			return existing;

		var result = await _client.Caches.CreateAsync(_model, new CreateCachedContentConfig
		{
			SystemInstruction = new Content { Parts = [new Part { Text = instructions }] },
			Ttl = $"{(int)_cacheTtl.TotalSeconds}s"
		}, cancellationToken);

		var name = result.Name ?? throw new InvalidOperationException("Gemini did not return a cached content name.");
		await _instructionCache.SetStringAsync(hash, name,
			new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _cacheTtl },
			cancellationToken);
		return name;
	}

	private string ComputeCacheKey(string text)
	{
		var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
		return $"{_model}:{Convert.ToHexString(bytes)}";
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
			contents.Add(new Content { Role = role, Parts = parts });
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
