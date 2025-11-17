using GeminiDotnet;
using GeminiDotnet.Extensions.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace nc.Ai.Tests;

public class Gemini
{

	public IConfigurationSection Configuration { get; }
	public GeminiClientOptions Options { get; }
	public IChatClient Client { get; }

	public Gemini()
	{
		Configuration = new ConfigurationBuilder()
			.AddUserSecrets("nc-hub")
			.Build()
			.GetSection("tests:nc-ai-tests:gemini");

		Options = new GeminiClientOptions { ApiKey = Configuration["apikey"]!, ModelId = Configuration["model"] };

		Client = new GeminiChatClient(Options);

		//Client = new ChatClient(Configuration["model"], Configuration["secretkey"])
		//	.AsIChatClient();
	}

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
		await foreach (var message in client.GetStreamingResponseAsync("What is the weather? Do I need an umbrella?", chatOptions))
		{
			response.AppendLine(message.Text);
		}
		Assert.NotNull(response.ToString());
	}

	[Fact]

	public async Task Embedding()
	{
		var options = new GeminiClientOptions
		{
			ApiKey = Configuration["apikey"]!,
			ModelId = Configuration["embeddingmodel"]!
		};
		IEmbeddingGenerator<string, Embedding<float>> generator =
			new GeminiEmbeddingGenerator(options);

		var embeddings = await generator.GenerateAsync("What is AI?");
		Assert.NotNull(embeddings);
		var vectors = string.Join(", ", embeddings.Vector.ToArray());
		Assert.NotEmpty(vectors);
	}

	[Fact]
	public async Task FileAnalysis()
	{
		var file = new UriContent("https://nascacht-io-sample.s3.us-east-1.amazonaws.com/financial/w2.pdf", "application/pdf");
		var question = new TextContent("What is the total amount in box 1?");
		var userMessage = new ChatMessage(
			ChatRole.User,
			new List<AIContent> { file, question } // Combining text and file content
		);
		var response = await Client.GetResponseAsync(new[] { userMessage });

	}
}
