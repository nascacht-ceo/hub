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
		Assert.True(answer.Contains("sunny") || answer.Contains("raining"), $"Answer was: {answer}");
	}

	[Fact]
	public async Task FileAnalysis()
	{
		// var file = new UriContent("https://nascacht-io-sample.s3.us-east-1.amazonaws.com/financial/w2.pdf", "application/pdf");
		// var file = new UriContent("https://nascacht-io-internal.s3.us-east-1.amazonaws.com/Combined.pdf?response-content-disposition=inline&X-Amz-Content-Sha256=UNSIGNED-PAYLOAD&X-Amz-Security-Token=IQoJb3JpZ2luX2VjEHQaCXVzLWVhc3QtMSJIMEYCIQD%2B2xqmz3XsLVjNnWOzcIkBsi5Y8qNcLdlYl17Lp%2FKgsgIhAPBLBhSyjgUJ15YUeF4uRxhME6s%2BnFLJTP0hk3Exjr2OKswDCD0QABoMMDY3NzUyNTUyOTgxIgxdsu7wadCp%2BCjx5CcqqQPkQ6kQNqDRQMVHlJqESi8VEKC8OWuvzvmsP8nG2bFcj%2FTwy1frcTlH%2BLUICIJ8g2i0kTcvibct%2FxompzYIx8Bugr7S%2FW72TDcjBQSJ6EYsfx5oUe4lQ85TwhYNzN6i91b5MtNtQZUM%2FfXbToWzJbTcwk66k5qfk9HwoS%2FrXXU%2Fwqybu8oPQ46C%2FFgH8S31jgWyzqS%2FNS0KzOdyIHGJBcYZxyXvo20HBhxZMvZsg5rE64motjq6VSPEDFWKNcy775Qt5XfE%2BOfHKBDRSq8t93cuy6Up%2FMuo7bbY5Hbp%2BmVqA0p9hFPoJq7xd1FnWfgjoTmtr6jTtu%2BbGoDtM%2FEyLJpXwGZT7%2FRK12jjjGx0M4YTt6vqzR9yrMDiHA5TOHMTBfOErUQ7Guxog2fLHjse9CUVzO5ZtwrrBRw9uP96LhvJOVA%2F5%2FlhDS1%2B3wln%2BE2fSnw7%2Ft8N09xmvxjRbw7xZ10%2BmTuliuPoZ3d3iUDkwFGnQdAlAIr71G4FOEXK2gch6BwjjN9Wv27wysupMqovWySZRrPg04WzY%2F00e9Pywa8Kq2vmwhBM6ueioDCA6c3MBjrdAud8OUfJbykohUs7oe85VZHXoAycb1OWHj07xVPu9u%2Fe4j46YVMGkX6TT%2Bt9rC3Applkf%2F3LwbQg2ThlaC5859FC43bgJ0IuEgDmaZS7QgiUQQzQS7P170YuEv0x%2BJGNlCr%2F3qRa2goOenwufT7hDJnybUmAOxa3VA2BgBblcW9t9ZuwzpiusbYHQY8skcWF1%2FoZUD5KLWp7n%2FhSbXOGFtacSq4rAx%2BKLPvB252%2BDnkbuSO5F4ycvNcMUwEBH1K5FWgu9TrN4RkmmqciEqbhbwxuLx5k1IDykQcjiYyQSU18s3OCYCsm7iBeSc0QgCcFV9oYZ4Yy97N0S0D2MtwHBn0S3NpuKs8IzBpZnmMzOYhur1kXBHX3FVU%2FUU0og475LNbenvsXqWM9Qm6lI2VrNgepiPN6VM4utUyubnzg6sqAwSqXEuVGleS%2BluBYSx%2BmnUtsMVyOKp9MmvtM9WQ%3D&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=ASIAQ7RS55IKYEZMSISY%2F20260216%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20260216T195000Z&X-Amz-Expires=43200&X-Amz-SignedHeaders=host&X-Amz-Signature=1c8506340954e0b1037e86935bcc439362b342cff9e6fce6c54bb0638f742aff", "application/pdf");
		var file = new UriContent("gs://nascacht-ai-tests/compound-c.pdf", "application/pdf");
		// https://storage.googleapis.com/nascacht-ai-tests/compound-c.pdf
		// var question = new TextContent("What is the total amount in box 1?");
		var question = new TextContent("How many pages in this document. Suggest a table of contents, including a title and start page.");
		var userMessage = new ChatMessage(
			ChatRole.User,
			[file, question] // Combining text and file content
		);
		var response = await Client.GetResponseAsync([userMessage]);
		Assert.Contains("44,629.35", response.Text);
	}
}
