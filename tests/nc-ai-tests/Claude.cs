using Anthropic.SDK;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace nc.Ai.Tests;

public class Claude: CommonTests
{
	public IConfigurationSection Configuration { get; }

	public Claude()
	{
		Configuration = new ConfigurationBuilder()
			.AddUserSecrets("nc-hub")
			.AddEnvironmentVariables("nc_hub__")
			.Build()
			.GetSection("tests:nc_ai_tests:claude");

		Client = new AnthropicClient(apiKeys: Configuration["apikey"]).Messages
			.AsBuilder()
			.ConfigureOptions(opts => opts.ModelId ??= Configuration["model"])
			.Use(inner => new UriContentDownloader(inner))
			.Build();
	}

	//[Fact]
	//public async Task Sample()
	//{
	//	var response = await Client.GetResponseAsync("What is AI?", new ChatOptions()
	//	{
	//		ModelId = Configuration["model"]!
	//	});
	//	Assert.NotNull(response);
	//}

	//[Fact]
	//public async Task FunctionCalling()
	//{
	//	ChatOptions chatOptions = new()
	//	{
	//		Tools = [AIFunctionFactory.Create(Functions.GetWeather)],
	//		ModelId = Configuration["model"]!
	//	};
	//	var client = ChatClientBuilderChatClientExtensions
	//		.AsBuilder(Client)
	//		.UseFunctionInvocation()
	//		.Build();
	//	var response = new StringBuilder();
	//	await foreach (var message in client.GetStreamingResponseAsync("What is the weather? Do I need an umbrella?", chatOptions))
	//	{
	//		response.AppendLine(message.Text);
	//	}
	//	Assert.NotNull(response.ToString());
	//}

	//[Fact(Skip ="Anthropic does not provide an embeddings endpoint.")]

	//public async Task Embedding()
	//{

	//}
}
