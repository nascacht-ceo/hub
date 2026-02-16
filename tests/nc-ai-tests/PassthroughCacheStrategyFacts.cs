using Microsoft.Extensions.AI;
using nc.Ai.Caching;

namespace nc.Ai.Tests;

public class PassthroughCacheStrategyFacts
{
	public class CreateCacheAsync : PassthroughCacheStrategyFacts
	{
		[Fact]
		public async Task ReturnsNonEmptyCacheId()
		{
			var strategy = new PassthroughCacheStrategy();

			var cacheId = await strategy.CreateCacheAsync(
				"You are a helpful assistant.", TimeSpan.FromMinutes(30));

			Assert.False(string.IsNullOrWhiteSpace(cacheId));
		}

		[Fact]
		public async Task MultipleCachesAreIndependent()
		{
			var strategy = new PassthroughCacheStrategy();
			var id1 = await strategy.CreateCacheAsync("Prompt A", TimeSpan.FromMinutes(5));
			var id2 = await strategy.CreateCacheAsync("Prompt B", TimeSpan.FromMinutes(5));

			Assert.NotEqual(id1, id2);

			var messages1 = new[]
			{
				new ChatMessage(ChatRole.System, [new CachedPromptReference(id1)])
			};
			var messages2 = new[]
			{
				new ChatMessage(ChatRole.System, [new CachedPromptReference(id2)])
			};

			var t1 = strategy.TransformMessages(messages1).ToList();
			var t2 = strategy.TransformMessages(messages2).ToList();

			Assert.Equal("Prompt A", ((TextContent)t1[0].Contents[0]).Text);
			Assert.Equal("Prompt B", ((TextContent)t2[0].Contents[0]).Text);

			await strategy.DeleteCacheAsync(id1);
			var t2Again = strategy.TransformMessages(messages2).ToList();
			Assert.Equal("Prompt B", ((TextContent)t2Again[0].Contents[0]).Text);
		}
	}

	public class DeleteCacheAsync : PassthroughCacheStrategyFacts
	{
		[Fact]
		public async Task RemovesCachedPrompt()
		{
			var strategy = new PassthroughCacheStrategy();
			var cacheId = await strategy.CreateCacheAsync(
				"You are a helpful assistant.", TimeSpan.FromMinutes(30));

			await strategy.DeleteCacheAsync(cacheId);

			var messages = new[]
			{
				new ChatMessage(ChatRole.System,
					[new CachedPromptReference(cacheId)])
			};

			Assert.Throws<InvalidOperationException>(
				() => strategy.TransformMessages(messages).ToList());
		}
	}

	public class TransformMessages : PassthroughCacheStrategyFacts
	{
		[Fact]
		public async Task ReplacesReferenceWithText()
		{
			var strategy = new PassthroughCacheStrategy();
			var systemPrompt = "You are a financial document analyst.";
			var cacheId = await strategy.CreateCacheAsync(
				systemPrompt, TimeSpan.FromMinutes(30));

			var messages = new[]
			{
				new ChatMessage(ChatRole.System,
					[new CachedPromptReference(cacheId)]),
				new ChatMessage(ChatRole.User,
					[new TextContent("Analyze this document.")])
			};

			var transformed = strategy.TransformMessages(messages).ToList();

			Assert.Equal(2, transformed.Count);

			var systemContents = transformed[0].Contents;
			Assert.Single(systemContents);
			var textContent = Assert.IsType<TextContent>(systemContents[0]);
			Assert.Equal(systemPrompt, textContent.Text);

			// User message untouched
			Assert.Same(messages[1], transformed[1]);
		}

		[Fact]
		public async Task PreservesNonCachedContent()
		{
			var strategy = new PassthroughCacheStrategy();
			var cacheId = await strategy.CreateCacheAsync(
				"Cached prompt text.", TimeSpan.FromMinutes(30));

			var messages = new[]
			{
				new ChatMessage(ChatRole.System, [
					new CachedPromptReference(cacheId),
					new TextContent("Additional instructions.")
				]),
				new ChatMessage(ChatRole.User,
					[new TextContent("Hello")])
			};

			var transformed = strategy.TransformMessages(messages).ToList();

			var systemContents = transformed[0].Contents;
			Assert.Equal(2, systemContents.Count);
			Assert.Equal("Cached prompt text.",
				((TextContent)systemContents[0]).Text);
			Assert.Equal("Additional instructions.",
				((TextContent)systemContents[1]).Text);
		}

		[Fact]
		public void ThrowsForUnknownCacheId()
		{
			var strategy = new PassthroughCacheStrategy();
			var messages = new[]
			{
				new ChatMessage(ChatRole.System,
					[new CachedPromptReference("nonexistent-id")])
			};

			Assert.Throws<InvalidOperationException>(
				() => strategy.TransformMessages(messages).ToList());
		}
	}
}
