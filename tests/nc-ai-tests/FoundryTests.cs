using Azure.AI.Inference;
using Azure.Core.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nc.Ai.Azure;
using nc.Ai.Interfaces;

namespace nc.Ai.Tests;

public class FoundryTests // : CommonTests, IAsyncLifetime
{
	private readonly ServiceProvider _services;
	private readonly AzureEventSourceListener _listener = AzureEventSourceListener.CreateConsoleLogger();

	public FoundryTests()
	{
		var configuration = new ConfigurationBuilder()
			.AddUserSecrets("nc-hub")
			.AddEnvironmentVariables("nc_hub__")
			.Build()
			.GetSection("tests:nc_ai_tests:azure");

		_services = new ServiceCollection()
			.AddAiFoundry("default", configuration)
			.BuildServiceProvider();
	}

	public Task InitializeAsync()
	{
		// Client = _services.GetRequiredService<IAgentManager>().GetChatClient("default");
		return Task.CompletedTask;
	}

	public async Task DisposeAsync()
	{
		await _services.DisposeAsync();
		_listener.Dispose();
	}
}
