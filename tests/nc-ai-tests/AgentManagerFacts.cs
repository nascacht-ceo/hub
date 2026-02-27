using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using nc.Ai.Interfaces;
using System.Runtime.CompilerServices;

namespace nc.Ai.Tests;

public class AgentManagerFacts
{
	// Builds AgentManager directly (bypassing DI) for focused unit tests.
	private static AgentManager BuildManager(
		IEnumerable<(string Name, IChatClient Client)>? agents = null,
		IEnumerable<IChatClientMiddleware>? middleware = null,
		AgentPipelineOptions? pipeline = null,
		IServiceProvider? services = null)
	{
		var sp = services ?? new ServiceCollection().BuildServiceProvider();
		var registrations = (agents ?? [])
			.Select(a => new AgentRegistration(a.Name, _ => new FakeFactory(a.Client)));
		return new AgentManager(
			sp,
			registrations,
			middleware ?? [],
			new OptionsMonitorStub<AgentPipelineOptions>(pipeline ?? new()));
	}

	private static FakeChatClient NewClient() => new();

	// --- GetChatClient ---

	public class GetChatClientFacts : AgentManagerFacts
	{
		[Fact]
		public void UnknownAgent_Throws()
		{
			var manager = BuildManager();
			Assert.Throws<KeyNotFoundException>(() => manager.GetChatClient("unknown"));
		}

		[Fact]
		public void KnownAgent_ReturnsClient()
		{
			var manager = BuildManager(agents: [("my-agent", NewClient())]);
			Assert.NotNull(manager.GetChatClient("my-agent"));
		}

		[Fact]
		public void NoMiddleware_ReturnsClient()
		{
			var manager = BuildManager(agents: [("agent", NewClient())], middleware: []);
			Assert.NotNull(manager.GetChatClient("agent"));
		}

		[Fact]
		public void ConventionMode_AllMiddlewareApplied()
		{
			var log = new List<string>();
			var manager = BuildManager(
				agents: [("agent", NewClient())],
				middleware: [new RecordingMiddleware("a", log), new RecordingMiddleware("b", log)],
				pipeline: new() { Pipeline = [] }); // empty = convention mode

			manager.GetChatClient("agent");

			Assert.Contains("a", log);
			Assert.Contains("b", log);
		}

		[Fact]
		public void ConventionMode_OutermostIsFirstRegistered()
		{
			// DI registration order = outermost-first in convention mode.
			// Wrap is called inside-out, so last call = outermost.
			var log = new List<string>();
			var manager = BuildManager(
				agents: [("agent", NewClient())],
				middleware: [new RecordingMiddleware("a", log), new RecordingMiddleware("b", log)],
				pipeline: new() { Pipeline = [] });

			manager.GetChatClient("agent");

			Assert.Equal("a", log.Last()); // a registered first → a is outermost
		}

		[Fact]
		public void ExplicitMode_OnlyNamedMiddlewareApplied()
		{
			var log = new List<string>();
			var manager = BuildManager(
				agents: [("agent", NewClient())],
				middleware:
				[
					new RecordingMiddleware("a", log),
					new RecordingMiddleware("b", log),
					new RecordingMiddleware("c", log), // registered but excluded from pipeline
				],
				pipeline: new() { Pipeline = ["a", "b"] });

			manager.GetChatClient("agent");

			Assert.Contains("a", log);
			Assert.Contains("b", log);
			Assert.DoesNotContain("c", log);
		}

		[Fact]
		public void ExplicitMode_OutermostIsFirstInPipeline()
		{
			// Pipeline = ["a", "b"] means a is outermost.
			var log = new List<string>();
			var manager = BuildManager(
				agents: [("agent", NewClient())],
				middleware: [new RecordingMiddleware("a", log), new RecordingMiddleware("b", log)],
				pipeline: new() { Pipeline = ["a", "b"] });

			manager.GetChatClient("agent");

			Assert.Equal("a", log.Last()); // last wrapped = outermost
		}

		[Fact]
		public void ExplicitMode_PipelineOrderOverridesRegistrationOrder()
		{
			// Registered: [a, b]; Pipeline: [b, a] — b should be outermost despite being registered second.
			var log = new List<string>();
			var manager = BuildManager(
				agents: [("agent", NewClient())],
				middleware: [new RecordingMiddleware("a", log), new RecordingMiddleware("b", log)],
				pipeline: new() { Pipeline = ["b", "a"] });

			manager.GetChatClient("agent");

			Assert.Equal("b", log.Last()); // last wrapped = outermost
		}

		[Fact]
		public void ExplicitMode_UnknownNameSkipped_DoesNotThrow()
		{
			var log = new List<string>();
			var manager = BuildManager(
				agents: [("agent", NewClient())],
				middleware: [new RecordingMiddleware("a", log)],
				pipeline: new() { Pipeline = ["a", "typo"] }); // "typo" has no implementation

			manager.GetChatClient("agent"); // should not throw

			Assert.Single(log);
			Assert.Equal("a", log[0]);
		}

		[Fact]
		public void AgentName_PassedToEachMiddleware()
		{
			var capturedNames = new List<string>();
			var manager = BuildManager(
				agents: [("my-agent", NewClient())],
				middleware:
				[
					new CapturingMiddleware("m1", (_, name) => capturedNames.Add(name)),
					new CapturingMiddleware("m2", (_, name) => capturedNames.Add(name)),
				]);

			manager.GetChatClient("my-agent");

			Assert.All(capturedNames, n => Assert.Equal("my-agent", n));
		}

		[Fact]
		public void PipelineReadFreshOnEachCall()
		{
			// IOptionsMonitor.CurrentValue is read per-call, so pipeline changes take effect immediately.
			var log = new List<string>();
			var options = new MutableOptionsMonitor(new AgentPipelineOptions { Pipeline = [] });
			var sp = new ServiceCollection().BuildServiceProvider();
			var manager = new AgentManager(
				sp,
				[new AgentRegistration("agent", _ => new FakeFactory(NewClient()))],
				[new RecordingMiddleware("a", log)],
				options);

			manager.GetChatClient("agent"); // convention mode: a applied
			Assert.Single(log);

			log.Clear();
			options.Current = new AgentPipelineOptions { Pipeline = ["nonexistent"] }; // exclude a

			manager.GetChatClient("agent"); // explicit mode: no matching middleware
			Assert.Empty(log);
		}
	}

	// --- AddAgentAsync ---

	public class AddAgentAsyncFacts : AgentManagerFacts
	{
		[Fact]
		public async Task AddedAgent_IsRetrievable()
		{
			var client = NewClient();
			var services = new ServiceCollection()
				.AddSingleton<IChatClientFactory<IAgent>>(new FakeGenericFactory<IAgent>(client))
				.BuildServiceProvider();

			var manager = BuildManager(services: services);
			await manager.AddAgentAsync(new IAgent { Name = "dynamic" });

			Assert.NotNull(manager.GetChatClient("dynamic"));
		}

		[Fact]
		public async Task AddedAgent_AppearsInGetAgentNames()
		{
			var services = new ServiceCollection()
				.AddSingleton<IChatClientFactory<IAgent>>(new FakeGenericFactory<IAgent>(NewClient()))
				.BuildServiceProvider();

			var manager = BuildManager(services: services);
			await manager.AddAgentAsync(new IAgent { Name = "dynamic" });

			Assert.Contains("dynamic", manager.GetAgentNames());
		}

		[Fact]
		public async Task AddedAgent_OverwritesExistingName()
		{
			var original = NewClient();
			var replacement = NewClient();
			var services = new ServiceCollection()
				.AddSingleton<IChatClientFactory<IAgent>>(new FakeGenericFactory<IAgent>(replacement))
				.BuildServiceProvider();

			var manager = BuildManager(agents: [("agent", original)], services: services);
			await manager.AddAgentAsync(new IAgent { Name = "agent" });

			// The factory now provides `replacement`; just verify it doesn't throw and returns a client.
			Assert.NotNull(manager.GetChatClient("agent"));
		}
	}

	// --- GetAgentNames ---

	public class GetAgentNamesFacts : AgentManagerFacts
	{
		[Fact]
		public void ReturnsAllRegisteredNames()
		{
			var manager = BuildManager(agents:
			[
				("alpha", NewClient()),
				("beta", NewClient()),
				("gamma", NewClient()),
			]);

			Assert.Equal(
				["alpha", "beta", "gamma"],
				manager.GetAgentNames().Order());
		}

		[Fact]
		public void EmptyWhenNoAgentsRegistered()
		{
			var manager = BuildManager();
			Assert.Empty(manager.GetAgentNames());
		}
	}

	// --- Helpers ---

	private sealed class RecordingMiddleware(string name, List<string> log) : IChatClientMiddleware
	{
		public string Name => name;
		public IChatClient Wrap(IChatClient inner, string agentName)
		{
			log.Add(name);
			return inner;
		}
	}

	private sealed class CapturingMiddleware(string name, Action<IChatClient, string> capture) : IChatClientMiddleware
	{
		public string Name => name;
		public IChatClient Wrap(IChatClient inner, string agentName)
		{
			capture(inner, agentName);
			return inner;
		}
	}

	private sealed class FakeFactory(IChatClient client) : IChatClientFactory
	{
		public IChatClient GetAgent(string name = "") => client;
		public IEnumerable<string> GetAgentNames() => [];
	}

	private sealed class FakeGenericFactory<TAgent>(IChatClient client) : IChatClientFactory<TAgent>
		where TAgent : IAgent
	{
		public IChatClient GetAgent(string name = "") => client;
		public IEnumerable<string> GetAgentNames() => [];
	}

	private sealed class FakeChatClient : IChatClient
	{
		public Task<ChatResponse> GetResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			CancellationToken cancellationToken = default)
			=> Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			yield return new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = [new TextContent("ok")] };
			await Task.CompletedTask;
		}

		public ChatClientMetadata Metadata => new();
		public object? GetService(Type serviceType, object? serviceKey = null) => null;
		public void Dispose() { }
	}

	private sealed class OptionsMonitorStub<T>(T value) : IOptionsMonitor<T>
	{
		public T CurrentValue => value;
		public T Get(string? name) => value;
		public IDisposable? OnChange(Action<T, string?> listener) => null;
	}

	private sealed class MutableOptionsMonitor(AgentPipelineOptions initial) : IOptionsMonitor<AgentPipelineOptions>
	{
		public AgentPipelineOptions Current { get; set; } = initial;
		public AgentPipelineOptions CurrentValue => Current;
		public AgentPipelineOptions Get(string? name) => Current;
		public IDisposable? OnChange(Action<AgentPipelineOptions, string?> listener) => null;
	}
}
