using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using nc.Ai.Gemini;
using System.Text;

namespace nc.Ai.Tests;

public class GeminiCachedChatClientFacts
{
	private readonly Client _genAiClient;
	private readonly GeminiChatClient _client;
	private readonly string _apiKey;
	private readonly string _model;

	private static readonly string SystemInstruction =
		"You analyze PDFs. Some PDFs may be stand-alone documents, others may be a concatenation of multiple documents. " +
		"For each document within the PDF, return a JSON node comprising the Title, Type of Document, StartPage and EndPage.";

	public GeminiCachedChatClientFacts()
	{
		var config = new ConfigurationBuilder()
			.AddUserSecrets("nc-hub")
			.AddEnvironmentVariables("nc_hub__")
			.Build()
			.GetSection("tests:nc_ai_tests:gemini");

		_apiKey = config["apikey"]!;
		_model = config["model"]!;
		_genAiClient = new Client(apiKey: _apiKey);
		_client = new GeminiChatClient(_genAiClient, new GeminiChatClientOptions { Model = _model });
	}

	public class GetResponseAsync : GeminiCachedChatClientFacts
	{
		[Fact]
		public async Task WithInstructionsReturnsResult()
		{
			var options = new ChatOptions { Instructions = SystemInstruction };
			var message = new ChatMessage(ChatRole.User, "What document types can you extract?");

			var response = await _client.GetResponseAsync([message], options);

			Assert.NotNull(response);
			Assert.False(string.IsNullOrWhiteSpace(response.Text));
		}

		[Fact]
		public async Task ReportsTokenUsage()
		{
			var options = new ChatOptions { Instructions = SystemInstruction };
			var message = new ChatMessage(ChatRole.User, "What fields do you extract?");

			var response = await _client.GetResponseAsync([message], options);

			Assert.NotNull(response.Usage);
			Assert.True(response.Usage.InputTokenCount > 0);
		}

		[Fact]
		public async Task WithUriContentAnalyzesDocument()
		{
			var options = new ChatOptions { Instructions = SystemInstruction };
			var message = new ChatMessage(ChatRole.User, [
				new UriContent(
					"https://nascacht-io-sample.s3.us-east-1.amazonaws.com/financial/w2.pdf",
					"application/pdf"),
				new TextContent("Extract the fields per your instructions.")
			]);

			var response = await _client.GetResponseAsync([message], options);

			Assert.NotNull(response);
			Assert.Contains("44", response.Text);
		}

		[Fact]
		public async Task MultipleCallsWithSameInstructionsSucceed()
		{
			var options = new ChatOptions { Instructions = SystemInstruction };
			var urls = new[]
			{
				"https://nascacht-io-sample.s3.us-east-1.amazonaws.com/financial/w2.pdf",
				"https://nascacht-io-sample.s3.us-east-1.amazonaws.com/financial/w2.pdf"
			};

			foreach (var url in urls)
			{
				var message = new ChatMessage(ChatRole.User, [
					new UriContent(url, "application/pdf"),
					new TextContent("Extract the fields per your instructions.")
				]);

				var response = await _client.GetResponseAsync([message], options);
				Assert.NotNull(response);
				Assert.False(string.IsNullOrWhiteSpace(response.Text));
			}
		}

		[Fact]
		public async Task Sample()
		{
			var uploader = new GeminiFileUploader(_client, _genAiClient);
			var config = new ConfigurationBuilder().AddUserSecrets("nc-hub").Build();
			foreach (var child in config.GetSection("sample:documents").GetChildren())
			{
				if (child.Value == null)
					continue;
				var file = new UriContent("https://storage.googleapis.com/nascacht-io-tests/bookmark.pdf", "application/pdf");
				var userMessage = new ChatMessage(ChatRole.User, [file]);
				var response = await uploader.GetResponseAsync([userMessage]);
				Assert.NotNull(response.Text);
			}
		}
	}

	public class GetStreamingResponseAsync : GeminiCachedChatClientFacts
	{
		[Fact]
		public async Task WithInstructionsStreamsTokens()
		{
			var options = new ChatOptions { Instructions = SystemInstruction };
			var message = new ChatMessage(ChatRole.User, "List the fields you extract.");
			var result = new StringBuilder();

			await foreach (var update in _client.GetStreamingResponseAsync([message], options))
			{
				result.Append(update.Text);
			}

			Assert.False(string.IsNullOrWhiteSpace(result.ToString()));
		}
	}

	public class HostedFileDiagnostic : GeminiCachedChatClientFacts
	{
		[Fact]
		public async Task UploadAndUseFile_SdkDirect_NoCache()
		{
			using var http = new HttpClient();
			var pdfUrl = "https://nascacht-io-sample.s3.us-east-1.amazonaws.com/financial/w2.pdf";
			var bytes = await http.GetByteArrayAsync(pdfUrl);

			var file = await _genAiClient.Files.UploadAsync(
				bytes, "w2.pdf",
				new UploadFileConfig { MimeType = "application/pdf" });

			while (file.State == FileState.Processing)
			{
				await Task.Delay(1000);
				file = await _genAiClient.Files.GetAsync(file.Name!);
			}

			var diag = $"Name={file.Name}, Uri={file.Uri}, State={file.State?.Value}, MimeType={file.MimeType}";

			Assert.Equal("ACTIVE", file.State?.Value);
			Assert.NotNull(file.Uri);

			var contents = new List<Content>
			{
				new()
				{
					Role = "user",
					Parts =
					[
						new Part { FileData = new FileData { FileUri = file.Uri, MimeType = "application/pdf" } },
						new Part { Text = "What is in box 1?" }
					]
				}
			};

			try
			{
				var response = await _genAiClient.Models.GenerateContentAsync(
					_model, contents, new GenerateContentConfig());
				var text = response.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
				Assert.False(string.IsNullOrWhiteSpace(text), $"Empty response. File: {diag}");
			}
			catch (Exception ex)
			{
				Assert.Fail($"GenerateContent failed. File: {diag}. Error: {ex.Message}");
			}
		}

		[Fact]
		public async Task UploadAndUseFile_WithInstructions()
		{
			using var http = new HttpClient();
			var pdfUrl = "https://nascacht-io-sample.s3.us-east-1.amazonaws.com/financial/w2.pdf";
			var bytes = await http.GetByteArrayAsync(pdfUrl);

			var file = await _genAiClient.Files.UploadAsync(
				bytes, "w2.pdf",
				new UploadFileConfig { MimeType = "application/pdf" });

			while (file.State == FileState.Processing)
			{
				await Task.Delay(1000);
				file = await _genAiClient.Files.GetAsync(file.Name!);
			}

			var diag = $"Name={file.Name}, Uri={file.Uri}, State={file.State?.Value}";
			var options = new ChatOptions { Instructions = SystemInstruction };
			var message = new ChatMessage(ChatRole.User, [
				new HostedFileContent(file.Uri!) { MediaType = "application/pdf" },
				new TextContent("Extract the fields per your instructions.")
			]);

			try
			{
				var response = await _client.GetResponseAsync([message], options);
				Assert.False(string.IsNullOrWhiteSpace(response.Text), $"Empty response. {diag}");
			}
			catch (Exception ex)
			{
				Assert.Fail($"GetResponseAsync failed. {diag}. Error: {ex.Message}");
			}
		}
	}
}
