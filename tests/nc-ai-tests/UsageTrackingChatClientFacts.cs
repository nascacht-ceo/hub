using Microsoft.Extensions.AI;
using nc.Ai;
using nc.Ai.Interfaces;
using System.Runtime.CompilerServices;

namespace nc.Ai.Tests;

public class UsageTrackingChatClientFacts
{
	public class GetResponseAsyncFacts : UsageTrackingChatClientFacts
	{
		[Fact]
		public async Task WithUsage_TracksRecord()
		{
			var tracker = new CapturingUsageTracker();
			var client = new UsageTrackingChatClient(new FakeChatClient("hi", inputTokens: 10, outputTokens: 5), tracker);

			await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")]);

			Assert.Single(tracker.Records);
			Assert.Equal(10, tracker.Records[0].InputTokens);
			Assert.Equal(5, tracker.Records[0].OutputTokens);
		}

		[Fact]
		public async Task WithoutUsage_DoesNotTrack()
		{
			var tracker = new CapturingUsageTracker();
			var client = new UsageTrackingChatClient(new FakeChatClient("hi", inputTokens: null, outputTokens: null), tracker);

			await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")]);

			Assert.Empty(tracker.Records);
		}

		[Fact]
		public async Task ConversationId_IncludedInRecord()
		{
			var tracker = new CapturingUsageTracker();
			var client = new UsageTrackingChatClient(new FakeChatClient("hi", inputTokens: 1, outputTokens: 1), tracker);

			await client.GetResponseAsync(
				[new ChatMessage(ChatRole.User, "hello")],
				new ChatOptions { ConversationId = "conv-1" });

			Assert.Equal("conv-1", tracker.Records[0].ConversationId);
		}

		[Fact]
		public async Task ConstructorTags_IncludedInRecord()
		{
			var tracker = new CapturingUsageTracker();
			var tags = new Dictionary<string, object?> { ["team"] = "product" };
			var client = new UsageTrackingChatClient(new FakeChatClient("hi", inputTokens: 1, outputTokens: 1), tracker, tags);

			await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")]);

			Assert.Equal("product", tracker.Records[0].Tags["team"]);
		}

		[Fact]
		public async Task AdditionalProperties_MergedWithConstructorTags()
		{
			var tracker = new CapturingUsageTracker();
			var tags = new Dictionary<string, object?> { ["team"] = "product" };
			var client = new UsageTrackingChatClient(new FakeChatClient("hi", inputTokens: 1, outputTokens: 1), tracker, tags);
			var options = new ChatOptions();
			options.AdditionalProperties = new AdditionalPropertiesDictionary { ["env"] = "prod" };

			await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options);

			Assert.Equal("product", tracker.Records[0].Tags["team"]);
			Assert.Equal("prod", tracker.Records[0].Tags["env"]);
		}
	}

	public class GetStreamingResponseAsyncFacts : UsageTrackingChatClientFacts
	{
		[Fact]
		public async Task StreamsAllUpdates()
		{
			var client = new UsageTrackingChatClient(new FakeChatClient("hi", inputTokens: null, outputTokens: null), new CapturingUsageTracker());
			var updates = new List<ChatResponseUpdate>();

			await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hello")]))
				updates.Add(update);

			Assert.Single(updates);
			Assert.Equal("hi", updates[0].Text);
		}
	}

	public class WithUsageTrackingFacts : UsageTrackingChatClientFacts
	{
		[Fact]
		public void ReturnsUsageTrackingChatClient()
		{
			var inner = new FakeChatClient("hi", inputTokens: null, outputTokens: null);
			var client = inner.WithUsageTracking(new CapturingUsageTracker());
			Assert.IsType<UsageTrackingChatClient>(client);
		}
	}

	// --- Helpers ---

	private sealed class CapturingUsageTracker : IUsageTracker
	{
		public List<UsageRecord> Records { get; } = [];

		public ValueTask TrackAsync(UsageRecord record, CancellationToken cancellationToken = default)
		{
			Records.Add(record);
			return ValueTask.CompletedTask;
		}
	}

	private sealed class FakeChatClient(string response, long? inputTokens, long? outputTokens) : IChatClient
	{
		public Task<ChatResponse> GetResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			var r = new ChatResponse(new ChatMessage(ChatRole.Assistant, response));
			if (inputTokens.HasValue || outputTokens.HasValue)
				r.Usage = new UsageDetails { InputTokenCount = inputTokens, OutputTokenCount = outputTokens };
			return Task.FromResult(r);
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			yield return new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = [new TextContent(response)] };
			await Task.CompletedTask;
		}

		public ChatClientMetadata Metadata => new();
		public object? GetService(Type serviceType, object? serviceKey = null) => null;
		public void Dispose() { }
	}
}
