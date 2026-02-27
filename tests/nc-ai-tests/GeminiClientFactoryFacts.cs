using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using nc.Ai.Gemini;
using nc.Ai.Interfaces;

namespace nc.Ai.Tests;

public class GeminiClientFactoryFacts
{
	private static ServiceProvider BuildProvider(Action<IServiceCollection>? configure = null)
	{
		var services = new ServiceCollection()
			.AddAiGemini("a", opts => { opts.Model = "gemini-2.0-flash"; opts.ApiKey = "key-a"; })
			.AddAiGemini("b", opts => { opts.Model = "gemini-2.5-pro"; opts.ApiKey = "key-b"; });
		configure?.Invoke(services);
		return services.BuildServiceProvider();
	}

	public class Get : GeminiClientFactoryFacts
	{
		[Fact]
		public void ReturnsClient()
		{
			using var sp = BuildProvider();
			var factory = sp.GetRequiredService<IChatClientFactory<GeminiAgent>>();

			var client = factory.GetAgent("a");

			Assert.NotNull(client);
			Assert.IsType<GeminiChatClient>(client);
		}

		[Fact]
		public void SameNameReturnsSameInstance()
		{
			using var sp = BuildProvider();
			var factory = sp.GetRequiredService<IChatClientFactory<GeminiAgent>>();

			var first = factory.GetAgent("a");
			var second = factory.GetAgent("a");

			Assert.Same(first, second);
		}

		[Fact]
		public void DifferentNamesReturnDifferentInstances()
		{
			using var sp = BuildProvider();
			var factory = sp.GetRequiredService<IChatClientFactory<GeminiAgent>>();

			var clientA = factory.GetAgent("a");
			var clientB = factory.GetAgent("b");

			Assert.NotSame(clientA, clientB);
		}

		[Fact]
		public void RuntimeAddedOptionsAreResolved()
		{
			using var sp = BuildProvider();
			sp.GetRequiredService<IOptionsMonitorCache<GeminiAgent>>()
				.TryAdd("runtime", new GeminiAgent { Model = "gemini-2.0-flash", ApiKey = "key-r" });

			// Verify the options monitor returns the injected agent, not a default instance.
			// We assert at the options level rather than constructing a full IChatClient to
			// avoid triggering the Google.GenAI.Client constructor, which detects ambient GCP
			// project/location from Application Default Credentials in CI environments and
			// throws when VertexAI is not explicitly set to true.
			var agent = sp.GetRequiredService<IOptionsMonitor<GeminiAgent>>().Get("runtime");

			Assert.Equal("gemini-2.0-flash", agent.Model);
			Assert.Equal("key-r", agent.ApiKey);
		}
	}
}
