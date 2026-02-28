using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using nc.Ai.Interfaces;
using System.Text;

namespace nc.Ai.Tests;

public abstract class CommonTests
{
	public required IChatClient Client { get; set; }

	[Fact]
	public async Task Sample()
	{
		var response = await Client.GetResponseAsync("What is AI?");
		Assert.NotNull(response);
	}

	[Fact]
	public async Task FunctionCalling()
	{
		ChatOptions chatOptions = new()
		{
			Tools = [AIFunctionFactory.Create(Functions.GetWeather)],
		};
		var client = ChatClientBuilderChatClientExtensions
			.AsBuilder(Client)
			.UseFunctionInvocation()
			.Build();
		var response = new StringBuilder();
		await foreach (var message in client.GetStreamingResponseAsync("What is the weather in New York today? Do I need an umbrella?", chatOptions))
		{
			response.AppendLine(message.Text);
		}
		var answer = response.ToString();
		var normalized = answer.Replace("\r", "").Replace("\n", "");
		Assert.True(normalized.Contains("sunny") || normalized.Contains("rain"), $"Answer was: {answer}");
	}

	[Fact]
	public virtual async Task FileAnalysis()
	{
		var file = new UriContent("https://nascacht-io-sample.s3.us-east-1.amazonaws.com/financial/w2.pdf", "application/pdf");
		var question = new TextContent("What is the total amount in box 1?");
		var userMessage = new ChatMessage(
			ChatRole.User,
			[file, question]
		);
		var response = await Client.GetResponseAsync([userMessage]);
		Assert.Contains("44,629.35", response.Text);
	}

	[Fact]
	public async Task UsesInstructions()
	{
		var options = new ChatOptions
		{
			Instructions = "Your name is Tiberius."
		};
		var response = await Client.GetResponseAsync("What is your name?", options);
		Assert.NotNull(response);
		Assert.Contains("Tiberius", response.Text);
	}

	[Fact]
	public async Task StaticAgentInstructions()
	{
		AgentInstructions instructions = "Your name is Tiberius.";
		var client = new InstructionsChatClient(Client, instructions);

		var response = await client.GetResponseAsync("What is your name?");

		Assert.Contains("Tiberius", response.Text);
	}

	[Fact]
	public async Task DynamicAgentInstructions()
	{
		var callCount = 0;
		Func<Task<string>> factory = async () =>
		{
			callCount++;
			await Task.Yield();
			return "Your name is Tiberius.";
		};
		AgentInstructions instructions = factory;
		var client = new InstructionsChatClient(Client, instructions);

		var first = await client.GetResponseAsync("What is your name?");
		var second = await client.GetResponseAsync("What is your name?");

		Assert.Contains("Tiberius", first.Text);
		Assert.Contains("Tiberius", second.Text);
		Assert.Equal(1, callCount);
	}

	[Fact]
	public async Task ConversationThread_RetainsHistory()
	{
		var client = Client.WithConversationThreads(NewConversationStore());

		var r1 = await client.GetResponseAsync("My name is Alice.");
		var r2 = await client.GetResponseAsync("What is my name?",
			new ChatOptions { ConversationId = r1.ConversationId });

		Assert.Contains("Alice", r2.Text, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task ConversationThread_Streaming_RetainsHistory()
	{
		var client = Client.WithConversationThreads(NewConversationStore());

		string? conversationId = null;
		await foreach (var update in client.GetStreamingResponseAsync("My name is Alice."))
			if (update.ConversationId is not null) conversationId = update.ConversationId;

		var r2 = await client.GetResponseAsync("What is my name?",
			new ChatOptions { ConversationId = conversationId });
		Assert.Contains("Alice", r2.Text, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task ConversationThreads_AreIsolated()
	{
		var client = Client.WithConversationThreads(NewConversationStore());

		var r1a = await client.GetResponseAsync("My name is Alice.");
		var r1b = await client.GetResponseAsync("My name is Bob.");

		var r2a = await client.GetResponseAsync("What is my name?",
			new ChatOptions { ConversationId = r1a.ConversationId });
		var r2b = await client.GetResponseAsync("What is my name?",
			new ChatOptions { ConversationId = r1b.ConversationId });

		Assert.Contains("Alice", r2a.Text, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("Bob", r2b.Text, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task TracksUsage()
	{
		var tracker = new CapturingUsageTracker();
		var client = Client.WithUsageTracking(tracker);

		await client.GetResponseAsync("What is 1+1?");

		Assert.Single(tracker.Records);
		Assert.True(tracker.Records[0].InputTokens > 0);
		Assert.True(tracker.Records[0].OutputTokens > 0);
	}

	[Fact]
	public virtual async Task TracksUsage_ConversationIdPropagated()
	{
		var tracker = new CapturingUsageTracker();
		var client = Client.WithUsageTracking(tracker);

		await client.GetResponseAsync("What is 1+1?",
			new ChatOptions { ConversationId = "conv-123" });

		Assert.Equal("conv-123", tracker.Records[0].ConversationId);
	}

	private static IConversationStore NewConversationStore()
	{
		var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
		return new DistributedCacheConversationStore(cache, new OptionsMonitorStub<ConversationStoreOptions>(new()));
	}

	private sealed class CapturingUsageTracker : IUsageTracker
	{
		public List<UsageRecord> Records { get; } = [];

		public ValueTask TrackAsync(UsageRecord record, CancellationToken cancellationToken = default)
		{
			Records.Add(record);
			return ValueTask.CompletedTask;
		}
	}

	private sealed class OptionsMonitorStub<T>(T value) : IOptionsMonitor<T>
	{
		public T CurrentValue => value;
		public T Get(string? name) => value;
		public IDisposable? OnChange(Action<T, string?> listener) => null;
	}
}
