using Anthropic.SDK;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace nc.Ai.Tests;

public class Claude
{
	public IConfigurationSection Configuration { get; }
	public IChatClient Client { get; }

	public Claude()
	{
		Configuration = new ConfigurationBuilder()
			.AddUserSecrets("nc-hub")
			.Build()
			.GetSection("tests:nc-ai-tests:claude");

		Client = new AnthropicClient(apiKeys: Configuration["apikey"]).Messages;
	}

	[Fact]
	public async Task Sample()
	{
		var response = await Client.GetResponseAsync("What is AI?", new ChatOptions()
		{
			ModelId = Configuration["model"]!
		});
		Assert.NotNull(response);
	}

	[Fact]
	public async Task FunctionCalling()
	{
		ChatOptions chatOptions = new()
		{
			Tools = [AIFunctionFactory.Create(Functions.GetWeather)],
			ModelId = Configuration["model"]!
		};
		var client = ChatClientBuilderChatClientExtensions
			.AsBuilder(Client)
			.UseFunctionInvocation()
			.Build();
		var response = new StringBuilder();
		await foreach (var message in client.GetStreamingResponseAsync("What is the weather? Do I need an umbrella?", chatOptions))
		{
			response.AppendLine(message.Text);
		}
		Assert.NotNull(response.ToString());
	}

	[Fact(Skip ="Anthropic does not provide an embeddings endpoint.")]

	public async Task Embedding()
	{

	}
}
