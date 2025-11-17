using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using OpenAI.Embeddings;
using System.Text;

namespace nc.Ai.Tests;

public class OpenAI
{
	public IConfigurationSection Configuration { get; }
	public IChatClient Client { get; }

	public OpenAI()
	{
		Configuration = new ConfigurationBuilder()
			.AddUserSecrets("nc-hub")
			.Build()
			.GetSection("tests:nc-ai-tests:openai");

		Client = new ChatClient(Configuration["model"], Configuration["secretkey"])
			.AsIChatClient();
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
			response.AppendLine(message.Text );
		}
		Assert.NotNull(response.ToString());
	}

	[Fact]

	public async Task Embedding()
	{
		IEmbeddingGenerator<string, Embedding<float>> generator =
			new EmbeddingClient(Configuration["embeddingmodel"], Configuration["secretkey"])
				.AsIEmbeddingGenerator();

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
		var userMessage = new Microsoft.Extensions.AI.ChatMessage(
			ChatRole.User,
			new List<AIContent> { file, question } // Combining text and file content
		);
		var response = await Client.GetResponseAsync(new[] { userMessage });

	}
}
