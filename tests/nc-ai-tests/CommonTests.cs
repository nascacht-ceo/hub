using Microsoft.Extensions.AI;
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
		await foreach (var message in client.GetStreamingResponseAsync("What is the weather? Do I need an umbrella?", chatOptions))
		{
			response.AppendLine(message.Text);
		}
		var answer = response.ToString();
		Assert.True(answer.Contains("sunny") || answer.Contains("rain"), $"Answer was: {answer}");
	}

	[Fact]
	public async Task FileAnalysis()
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
}
