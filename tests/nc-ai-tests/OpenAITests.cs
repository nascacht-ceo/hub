using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nc.Ai.Interfaces;
using nc.Ai.OpenAI;
using OpenAI.Embeddings;

namespace nc.Ai.Tests;

public class OpenAITests : CommonTests, IAsyncLifetime
{
	private readonly IConfigurationSection _configuration;
	private readonly ServiceProvider _services;

	public OpenAITests()
	{
		_configuration = new ConfigurationBuilder()
			.AddUserSecrets("nc-hub")
			.AddEnvironmentVariables("nc_hub__")
			.Build()
			.GetSection("tests:nc_ai_tests:openai");

		_services = new ServiceCollection()
			.AddAiOpenAI("default", opts =>
			{
				opts.Model = _configuration["model"] ?? "gpt-4o";
				opts.ApiKey = _configuration["secretkey"];
			})
			.BuildServiceProvider();
	}

	public Task InitializeAsync()
	{
		Client = _services.GetRequiredService<IAgentManager>().GetChatClient("default");
		return Task.CompletedTask;
	}

	public async Task DisposeAsync() => await _services.DisposeAsync();

	[Fact]
	public async Task Embedding()
	{
		IEmbeddingGenerator<string, Embedding<float>> generator =
			new EmbeddingClient(_configuration["embeddingmodel"], _configuration["secretkey"])
				.AsIEmbeddingGenerator();

		var embeddings = await generator.GenerateAsync("What is AI?");

		Assert.NotNull(embeddings);
		var vectors = string.Join(", ", embeddings.Vector.ToArray());
		Assert.NotEmpty(vectors);
	}
}
