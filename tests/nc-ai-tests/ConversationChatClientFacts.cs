using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using nc.Ai.Interfaces;
using System.Runtime.CompilerServices;

namespace nc.Ai.Tests;

public class ConversationChatClientFacts
{
	private static MemoryDistributedCache NewCache() =>
		new(Options.Create(new MemoryDistributedCacheOptions()));

	private static DistributedCacheConversationStore NewStore(IDistributedCache? cache = null) =>
		new(cache ?? NewCache(), new OptionsMonitorStub<ConversationStoreOptions>(new()));

	private static SlidingWindowCompactionStrategy NoCompaction() =>
		new(new SlidingWindowOptions { MaxMessages = int.MaxValue });

	public class GetResponseAsyncFacts : ConversationChatClientFacts
	{
		[Fact]
		public async Task NoConversationId_GeneratesConversationId()
		{
			var client = new ConversationChatClient(new FakeChatClient("hi"), NewStore(), NoCompaction());

			var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")]);

			Assert.NotNull(response.ConversationId);
			Assert.NotEmpty(response.ConversationId);
		}

		[Fact]
		public async Task NoConversationId_SavesHistory()
		{
			var store = new TrackingConversationStore();
			var client = new ConversationChatClient(new FakeChatClient("hi"), store, NoCompaction());

			await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")]);

			Assert.Equal(1, store.LoadCount);
			Assert.Equal(1, store.SaveCount);
		}

		[Fact]
		public async Task WithConversationId_EchoesIdInResponse()
		{
			var client = new ConversationChatClient(new FakeChatClient("world"), NewStore(), NoCompaction());
			var options = new ChatOptions { ConversationId = "thread-1" };

			var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options);

			Assert.Equal("thread-1", response.ConversationId);
		}

		[Fact]
		public async Task WithConversationId_SavesUserAndAssistantMessages()
		{
			var store = NewStore();
			var client = new ConversationChatClient(new FakeChatClient("world"), store, NoCompaction());
			var options = new ChatOptions { ConversationId = "thread-1" };

			await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options);

			var saved = await store.LoadAsync("thread-1");
			Assert.Equal(2, saved.Count);
			Assert.Equal("hello", saved[0].Text);
			Assert.Equal("world", saved[1].Text);
		}

		[Fact]
		public async Task GeneratedId_UsedForSubsequentCall_RetainsHistory()
		{
			var store = NewStore();
			var inner = new FakeChatClient("first reply");
			var client = new ConversationChatClient(inner, store, NoCompaction());

			var r1 = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "first")]);
			inner.Reset("second reply");

			var options = new ChatOptions { ConversationId = r1.ConversationId };
			await client.GetResponseAsync([new ChatMessage(ChatRole.User, "second")], options);

			Assert.Equal(3, inner.ReceivedMessages!.Count);
			Assert.Equal("first", inner.ReceivedMessages[0].Text);
			Assert.Equal("first reply", inner.ReceivedMessages[1].Text);
			Assert.Equal("second", inner.ReceivedMessages[2].Text);
		}

		[Fact]
		public async Task SecondTurn_PrependsStoredHistory()
		{
			var store = NewStore();
			var inner = new FakeChatClient("first reply");
			var client = new ConversationChatClient(inner, store, NoCompaction());
			var options = new ChatOptions { ConversationId = "thread-1" };

			await client.GetResponseAsync([new ChatMessage(ChatRole.User, "first")], options);
			inner.Reset("second reply");

			await client.GetResponseAsync([new ChatMessage(ChatRole.User, "second")], options);

			Assert.Equal(3, inner.ReceivedMessages!.Count);
			Assert.Equal("first", inner.ReceivedMessages[0].Text);
			Assert.Equal("first reply", inner.ReceivedMessages[1].Text);
			Assert.Equal("second", inner.ReceivedMessages[2].Text);
		}

		[Fact]
		public async Task ConversationIdNotForwardedToInner()
		{
			var inner = new FakeChatClient("reply");
			var client = new ConversationChatClient(inner, NewStore(), NoCompaction());
			var options = new ChatOptions { ConversationId = "thread-1" };

			await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")], options);

			Assert.Null(inner.ReceivedConversationId);
		}

		[Fact]
		public async Task Compaction_TrimsOldMessagesBeforeSending()
		{
			var store = NewStore();
			var inner = new FakeChatClient("reply");
			var compaction = new SlidingWindowCompactionStrategy(new SlidingWindowOptions { MaxMessages = 2 });
			var client = new ConversationChatClient(inner, store, compaction);
			var options = new ChatOptions { ConversationId = "thread-1" };

			await store.SaveAsync("thread-1", [
				new ChatMessage(ChatRole.User, "old-1"),
				new ChatMessage(ChatRole.Assistant, "old-reply-1"),
				new ChatMessage(ChatRole.User, "old-2"),
				new ChatMessage(ChatRole.Assistant, "old-reply-2"),
			]);

			await client.GetResponseAsync([new ChatMessage(ChatRole.User, "new")], options);

			// stored=4, incoming=1 → full=5, compacted to MaxMessages=2 → TakeLast(2) = [old-reply-2, new]
			Assert.Equal(2, inner.ReceivedMessages!.Count);
			Assert.Equal("old-reply-2", inner.ReceivedMessages[0].Text);
			Assert.Equal("new", inner.ReceivedMessages[1].Text);
		}

		[Fact]
		public async Task Compaction_SystemMessagesAlwaysPreserved()
		{
			var store = NewStore();
			var inner = new FakeChatClient("reply");
			var compaction = new SlidingWindowCompactionStrategy(new SlidingWindowOptions { MaxMessages = 3 });
			var client = new ConversationChatClient(inner, store, compaction);
			var options = new ChatOptions { ConversationId = "thread-1" };

			await store.SaveAsync("thread-1", [
				new ChatMessage(ChatRole.System, "You are helpful."),
				new ChatMessage(ChatRole.User, "old"),
				new ChatMessage(ChatRole.Assistant, "old-reply"),
				new ChatMessage(ChatRole.User, "older"),
				new ChatMessage(ChatRole.Assistant, "older-reply"),
			]);

			await client.GetResponseAsync([new ChatMessage(ChatRole.User, "new")], options);

			// system=1, MaxMessages=3, keepCount=2 → system + TakeLast(2) = [system, older-reply, new]
			Assert.Equal(3, inner.ReceivedMessages!.Count);
			Assert.Equal(ChatRole.System, inner.ReceivedMessages[0].Role);
		}

		[Fact]
		public async Task NativeMode_NoConversationId_StoreNotAccessed()
		{
			var store = new TrackingConversationStore();
			var client = new ConversationChatClient(new NativeFakeChatClient("hi"), store, NoCompaction());

			await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")]);

			Assert.Equal(0, store.LoadCount);
			Assert.Equal(0, store.SaveCount);
		}

		[Fact]
		public async Task NativeMode_WithConversationId_StoreNotAccessed()
		{
			var store = new TrackingConversationStore();
			var client = new ConversationChatClient(new NativeFakeChatClient("hi"), store, NoCompaction());
			var options = new ChatOptions { ConversationId = "resp_abc123" };

			await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options);

			Assert.Equal(0, store.LoadCount);
			Assert.Equal(0, store.SaveCount);
		}

		[Fact]
		public async Task NativeMode_ConversationIdForwardedToInner()
		{
			var inner = new NativeFakeChatClient("hi");
			var client = new ConversationChatClient(inner, NewStore(), NoCompaction());
			var options = new ChatOptions { ConversationId = "resp_abc123" };

			await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options);

			Assert.Equal("resp_abc123", inner.ReceivedConversationId);
		}
	}

	public class GetStreamingResponseAsyncFacts : ConversationChatClientFacts
	{
		[Fact]
		public async Task NoConversationId_GeneratesConversationId()
		{
			var client = new ConversationChatClient(new FakeChatClient("hi"), NewStore(), NoCompaction());
			var updates = new List<ChatResponseUpdate>();

			await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hello")]))
				updates.Add(update);

			Assert.Contains(updates, u => u.ConversationId is not null);
		}

		[Fact]
		public async Task WithConversationId_EchoesIdInFinalUpdate()
		{
			var client = new ConversationChatClient(new FakeChatClient("streamed"), NewStore(), NoCompaction());
			var options = new ChatOptions { ConversationId = "thread-1" };
			var updates = new List<ChatResponseUpdate>();

			await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")], options))
				updates.Add(update);

			Assert.Equal("thread-1", updates.Last().ConversationId);
		}

		[Fact]
		public async Task WithConversationId_PersistsAfterStreamCompletes()
		{
			var store = NewStore();
			var client = new ConversationChatClient(new FakeChatClient("streamed"), store, NoCompaction());
			var options = new ChatOptions { ConversationId = "thread-1" };

			await foreach (var _ in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")], options))
			{ }

			var saved = await store.LoadAsync("thread-1");
			Assert.Equal(2, saved.Count);
			Assert.Equal("hi", saved[0].Text);
			Assert.Equal("streamed", saved[1].Text);
		}

		[Fact]
		public async Task NativeMode_StoreNotAccessed()
		{
			var store = new TrackingConversationStore();
			var client = new ConversationChatClient(new NativeFakeChatClient("hi"), store, NoCompaction());

			await foreach (var _ in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hello")]))
			{ }

			Assert.Equal(0, store.LoadCount);
			Assert.Equal(0, store.SaveCount);
		}
	}

	public class WithConversationThreadsFacts : ConversationChatClientFacts
	{
		[Fact]
		public void ReturnsConversationChatClient()
		{
			var wrapped = new FakeChatClient("hi").WithConversationThreads(NewStore());
			Assert.IsType<ConversationChatClient>(wrapped);
		}
	}

	// --- Helpers ---

	private sealed class FakeChatClient(string response) : IChatClient
	{
		public IReadOnlyList<ChatMessage>? ReceivedMessages { get; private set; }
		public string? ReceivedConversationId { get; private set; }

		public void Reset(string newResponse) => response = newResponse;

		public Task<ChatResponse> GetResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			ReceivedMessages = messages.ToList();
			ReceivedConversationId = options?.ConversationId;
			return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, response)));
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			ReceivedMessages = messages.ToList();
			ReceivedConversationId = options?.ConversationId;
			yield return new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = [new TextContent(response)] };
			await Task.CompletedTask;
		}

		public ChatClientMetadata Metadata => new();
		public object? GetService(Type serviceType, object? serviceKey = null) => null;
		public void Dispose() { }
	}

	private sealed class NativeFakeChatClient(string response) : IChatClient, INativeConversations
	{
		public string? ReceivedConversationId { get; private set; }

		public Task<ChatResponse> GetResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			ReceivedConversationId = options?.ConversationId;
			return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, response)));
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			ReceivedConversationId = options?.ConversationId;
			yield return new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = [new TextContent(response)] };
			await Task.CompletedTask;
		}

		public ChatClientMetadata Metadata => new();
		public object? GetService(Type serviceType, object? serviceKey = null)
			=> serviceType == typeof(INativeConversations) ? this : null;
		public void Dispose() { }
	}

	private sealed class TrackingConversationStore : IConversationStore
	{
		public int LoadCount { get; private set; }
		public int SaveCount { get; private set; }

		public Task<IReadOnlyList<ChatMessage>> LoadAsync(string threadId, CancellationToken cancellationToken = default)
		{
			LoadCount++;
			return Task.FromResult<IReadOnlyList<ChatMessage>>([]);
		}

		public Task SaveAsync(string threadId, IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken = default)
		{
			SaveCount++;
			return Task.CompletedTask;
		}
	}

	private sealed class OptionsMonitorStub<T>(T value) : IOptionsMonitor<T>
	{
		public T CurrentValue => value;
		public T Get(string? name) => value;
		public IDisposable? OnChange(Action<T, string?> listener) => null;
	}
}
