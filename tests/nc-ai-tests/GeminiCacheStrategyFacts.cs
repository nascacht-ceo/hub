using Google.GenAI;
using Microsoft.Extensions.AI;
using nc.Ai.Caching;
using nc.Ai.Gemini;

namespace nc.Ai.Tests;

public class GeminiCacheStrategyFacts
{
	public class TransformMessages : GeminiCacheStrategyFacts
	{
		[Fact]
		public void StripsCachedPromptReference()
		{
			var geminiClient = new GeminiCachedChatClient(
				new Client(apiKey: "fake-key"), "fake-model");
			var strategy = new GeminiCacheStrategy(geminiClient);

			var messages = new[]
			{
				new ChatMessage(ChatRole.System,
					[new CachedPromptReference("cachedContents/abc123")]),
				new ChatMessage(ChatRole.User,
					[new TextContent("Analyze this document.")])
			};

			var transformed = strategy.TransformMessages(messages).ToList();

			// System message was only a CachedPromptReference — dropped entirely
			Assert.Single(transformed);
			Assert.Equal(ChatRole.User, transformed[0].Role);
		}

		[Fact]
		public void PreservesNonCachedContent()
		{
			var geminiClient = new GeminiCachedChatClient(
				new Client(apiKey: "fake-key"), "fake-model");
			var strategy = new GeminiCacheStrategy(geminiClient);

			var messages = new[]
			{
				new ChatMessage(ChatRole.System, [
					new CachedPromptReference("cachedContents/abc123"),
					new TextContent("Additional system text.")
				]),
				new ChatMessage(ChatRole.User,
					[new TextContent("Hello")])
			};

			var transformed = strategy.TransformMessages(messages).ToList();

			Assert.Equal(2, transformed.Count);
			Assert.Single(transformed[0].Contents);
			Assert.Equal("Additional system text.",
				((TextContent)transformed[0].Contents[0]).Text);
		}

		[Fact]
		public void PassesThroughWhenNoCachedRef()
		{
			var geminiClient = new GeminiCachedChatClient(
				new Client(apiKey: "fake-key"), "fake-model");
			var strategy = new GeminiCacheStrategy(geminiClient);

			var messages = new[]
			{
				new ChatMessage(ChatRole.System,
					[new TextContent("Normal system prompt.")]),
				new ChatMessage(ChatRole.User,
					[new TextContent("Hello")])
			};

			var transformed = strategy.TransformMessages(messages).ToList();

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
			var geminiClient = new GeminiCachedChatClient(
				new Client(apiKey: "fake-key"), "fake-model");
			var strategy = new GeminiCacheStrategy(geminiClient);

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

			var transformed = strategy.TransformMessages(messages).ToList();

			Assert.Equal(2, transformed.Count);
			Assert.Equal(ChatRole.System, transformed[0].Role);
			Assert.Single(transformed[0].Contents);
			Assert.IsType<TextContent>(transformed[0].Contents[0]);
			Assert.Equal("Short prompt",
				((TextContent)transformed[0].Contents[0]).Text);
		}
	}
}
