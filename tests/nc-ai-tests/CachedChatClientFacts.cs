using Amazon.Runtime.Internal.Util;
using Google.GenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nc.Ai.Caching;
using nc.Ai.Gemini;
using System.Runtime.CompilerServices;
using System.Text;

namespace nc.Ai.Tests;

public class CachedChatClientFacts
{
	private readonly IAiContextCache _cache;

	public CachedChatClientFacts()
	{
		var configuration = new ConfigurationBuilder().AddJsonFile("tests.json").Build();
		var services = new ServiceCollection()
			.Configure<AiContextCacheOptions>(configuration)
			.AddSingleton<IDistributedCache, MemoryDistributedCache>()
			.AddSingleton<IAiContextCache, AiContextCache>()
			.BuildServiceProvider();
		_cache = services.GetRequiredService<IAiContextCache>();
	}
	internal class StubChatClient : IChatClient
	{
		public IEnumerable<ChatMessage>? LastMessages { get; private set; }
		public string ResponseText { get; set; } = "stub response";

		public Task<ChatResponse> GetResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			LastMessages = messages.ToList();
			return Task.FromResult(
				new ChatResponse(new ChatMessage(ChatRole.Assistant, ResponseText)));
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			LastMessages = messages.ToList();
			yield return new ChatResponseUpdate(ChatRole.Assistant, ResponseText);
			await Task.CompletedTask;
		}

		public void Dispose() { }
		public object? GetService(Type serviceType, object? serviceKey = null) => null;
	}

	public class GetResponseAsync : CachedChatClientFacts
	{
		[Fact]
		public async Task ExpandsPromptWithPassthrough()
		{
			var stub = new StubChatClient();
			var strategy = new PassthroughCacheStrategy();
			var client = new CachedChatClient(stub, strategy);

			var systemPrompt = "You are an expert analyst.";
			var cacheId = await strategy.CreateCacheAsync(
				systemPrompt, TimeSpan.FromMinutes(30));

			var response = await client.GetResponseAsync([
				new ChatMessage(ChatRole.System,
					[new CachedPromptReference(cacheId)]),
				new ChatMessage(ChatRole.User,
					[new TextContent("Analyze this.")])
			]);

			Assert.Equal("stub response", response.Text);

			var received = stub.LastMessages!.ToList();
			Assert.Equal(2, received.Count);
			var systemContent = received[0].Contents[0];
			Assert.IsType<TextContent>(systemContent);
			Assert.Equal(systemPrompt, ((TextContent)systemContent).Text);
		}
	}

	public class GetStreamingResponseAsync : CachedChatClientFacts
	{
		[Fact]
		public async Task ExpandsPromptWithPassthrough()
		{
			var stub = new StubChatClient();
			var strategy = new PassthroughCacheStrategy();
			var client = new CachedChatClient(stub, strategy);

			var systemPrompt = "You are a streaming test assistant.";
			var cacheId = await strategy.CreateCacheAsync(
				systemPrompt, TimeSpan.FromMinutes(30));

			var result = new StringBuilder();
			await foreach (var update in client.GetStreamingResponseAsync([
				new ChatMessage(ChatRole.System,
					[new CachedPromptReference(cacheId)]),
				new ChatMessage(ChatRole.User,
					[new TextContent("Stream test.")])
			]))
			{
				result.Append(update.Text);
			}

			Assert.Equal("stub response", result.ToString());

			var received = stub.LastMessages!.ToList();
			Assert.IsType<TextContent>(received[0].Contents[0]);
		}
	}

	public class Integration : CachedChatClientFacts
	{
		[Fact]
		public async Task GeminiEndToEnd()
		{
			var config = new ConfigurationBuilder()
				.AddUserSecrets("nc-hub")
				.AddEnvironmentVariables("nc_hub__")
				.Build()
				.GetSection("tests:nc_ai_tests:gemini");

			var apiKey = config["apikey"];
			var model = config["model"];

			if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(model))
				return;

			var geminiClient = new GeminiChatClient(_cache, 
				new Client(apiKey: apiKey), model);
			var strategy = new GeminiCacheStrategy(geminiClient, new PassthroughCacheStrategy());
			var client = new CachedChatClient(geminiClient, strategy);

			var systemPrompt = """
				You are a financial document analyst. For every document provided,
				extract the following fields and return them as JSON:
				- document_type
				- tax_year
				- employer_name
				Always respond with valid JSON only.
				""";

			string cacheId;
			try
			{
				cacheId = await strategy.CreateCacheAsync(
					systemPrompt, TimeSpan.FromMinutes(5));
			}
			catch (HttpRequestException)
			{
				return; // API unavailable
			}

			try
			{
				Assert.NotNull(cacheId);

				var response = await client.GetResponseAsync([
					new ChatMessage(ChatRole.System,
						[new CachedPromptReference(cacheId)]),
					new ChatMessage(ChatRole.User,
						[new TextContent("What document types can you extract?")])
				]);

				Assert.NotNull(response);
				Assert.False(string.IsNullOrWhiteSpace(response.Text));
			}
			finally
			{
				await strategy.DeleteCacheAsync(cacheId);
			}
		}
	}
}
