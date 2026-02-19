using Amazon.Runtime.Internal.Util;
using Google.GenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nc.Ai.Caching;
using nc.Ai.Gemini;

namespace nc.Ai.Tests;

public class GeminiCacheStrategyFacts
{
	private readonly IAiContextCache _cache;

	public GeminiCacheStrategyFacts()
	{
		var configuration = new ConfigurationBuilder().AddJsonFile("tests.json").Build();
		var services = new ServiceCollection()
			.Configure<AiContextCacheOptions>(configuration)
			.AddSingleton<IDistributedCache, MemoryDistributedCache>()
			.AddSingleton<IAiContextCache, AiContextCache>()
			.BuildServiceProvider();
		_cache = services.GetRequiredService<IAiContextCache>();
	}
	public class TransformMessages : GeminiCacheStrategyFacts
	{
		[Fact]
		public async Task StripsCachedPromptReference()
		{
			var geminiClient = new GeminiChatClient(_cache,
				new Client(apiKey: "fake-key"), "fake-model");
			var strategy = new GeminiCacheStrategy(geminiClient, new PassthroughCacheStrategy());

			var messages = new[]
			{
				new ChatMessage(ChatRole.System,
					[new CachedPromptReference("cachedContents/abc123")]),
				new ChatMessage(ChatRole.User,
					[new TextContent("Analyze this document.")])
			};

			var transformed = await strategy.TransformMessages(messages).ToListAsync();

			// System message was only a CachedPromptReference — dropped entirely
			Assert.Single(transformed);
			Assert.Equal(ChatRole.User, transformed[0].Role);
		}

		[Fact]
		public async Task PreservesNonCachedContent()
		{
			var geminiClient = new GeminiChatClient(_cache,
				new Client(apiKey: "fake-key"), "fake-model");
			var strategy = new GeminiCacheStrategy(geminiClient, new PassthroughCacheStrategy());

			var messages = new[]
			{
				new ChatMessage(ChatRole.System, [
					new CachedPromptReference("cachedContents/abc123"),
					new TextContent("Additional system text.")
				]),
				new ChatMessage(ChatRole.User,
					[new TextContent("Hello")])
			};

			var transformed = await strategy.TransformMessages(messages).ToListAsync();

			Assert.Equal(2, transformed.Count);
			Assert.Single(transformed[0].Contents);
			Assert.Equal("Additional system text.",
				((TextContent)transformed[0].Contents[0]).Text);
		}

		[Fact]
		public async Task PassesThroughWhenNoCachedRef()
		{
			var geminiClient = new GeminiChatClient(_cache,
				new Client(apiKey: "fake-key"), "fake-model");
			var strategy = new GeminiCacheStrategy(geminiClient, new PassthroughCacheStrategy());

			var messages = new[]
			{
				new ChatMessage(ChatRole.System,
					[new TextContent("Normal system prompt.")]),
				new ChatMessage(ChatRole.User,
					[new TextContent("Hello")])
			};

			var transformed = await strategy.TransformMessages(messages).ToListAsync();

			Assert.Equal(2, transformed.Count);
			Assert.Same(messages[0], transformed[0]);
			Assert.Same(messages[1], transformed[1]);
		}
	}

	public class CreateCacheAsync : GeminiCacheStrategyFacts
	{
		[Fact]
		public async Task FallsBackToPassthroughForSmallContent()
		{
			var geminiClient = new GeminiChatClient(_cache,
				new Client(apiKey: "fake-key"), "fake-model");
			var strategy = new GeminiCacheStrategy(geminiClient, new PassthroughCacheStrategy());

			// Prompt with fewer than MinWordCount words uses passthrough fallback
			// without ever calling the Gemini API.
			var cacheId = await strategy.CreateCacheAsync(
				"Short prompt", TimeSpan.FromMinutes(5));

			Assert.NotNull(cacheId);

			// After fallback, TransformMessages should expand the reference
			// (passthrough behavior) instead of stripping it (Gemini behavior).
			var messages = new[]
			{
				new ChatMessage(ChatRole.System,
					[new CachedPromptReference(cacheId)]),
				new ChatMessage(ChatRole.User,
					[new TextContent("Hello")])
			};

			var transformed = await strategy.TransformMessages(messages).ToListAsync();

			Assert.Equal(2, transformed.Count);
			Assert.Equal(ChatRole.System, transformed[0].Role);
			Assert.Single(transformed[0].Contents);
			Assert.IsType<TextContent>(transformed[0].Contents[0]);
			Assert.Equal("Short prompt",
				((TextContent)transformed[0].Contents[0]).Text);
		}
	}
}
