using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nc.Ai.Anthropic;
using nc.Ai.Interfaces;

namespace nc.Ai.Tests;

public class ClaudeTests : CommonTests, IAsyncLifetime
{
	private readonly ServiceProvider _services;

	public ClaudeTests()
	{
		var configuration = new ConfigurationBuilder()
			.AddUserSecrets("nc-hub")
			.AddEnvironmentVariables("nc_hub__")
			.Build()
			.GetSection("tests:nc_ai_tests:claude");

		_services = new ServiceCollection()
			.AddLogging()
			.AddUsageTracking()
			.AddAiClaude("default", opts =>
			{
				opts.Model = configuration["model"] ?? "claude-opus-4-5";
				opts.ApiKey = configuration["apikey"];
			})
			.BuildServiceProvider();
	}

	public Task InitializeAsync()
	{
		Client = _services.GetRequiredService<IAgentManager>().GetChatClient("default");
		return Task.CompletedTask;
	}

	public async Task DisposeAsync() => await _services.DisposeAsync();
}
