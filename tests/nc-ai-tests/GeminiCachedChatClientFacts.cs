using Amazon.Runtime.Internal.Util;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nc.Ai.Gemini;
using System.Text;

namespace nc.Ai.Tests;

public class GeminiCachedChatClientFacts : IAsyncLifetime
{
	private readonly Client _genAiClient;
	private readonly GeminiChatClient _client;
	private readonly IAiContextCache _cache;
	private readonly string _apiKey;
	private readonly string _model;

	/// <summary>
	/// Gemini requires at least 2,048 tokens for cached content.
	/// This prompt is generated to exceed that minimum.
	/// </summary>
	private static readonly string SystemInstruction = BuildSystemInstruction();

	private static string BuildSystemInstruction()
	{
		return "You analyze PDFs. Some PDFs may be stand-alone documents, others may be a concatenation of multipld documents." +
			"Foreach document within the PDF, return a JSON node comprising the Title, Type of Document, StartPage and EndPage.";
		

		//var sb = new StringBuilder();
		//sb.AppendLine("""
		//	You are a financial document analyst. For every document provided, extract
		//	the relevant fields and return them as JSON.
		//	Always respond with valid JSON only, no markdown fences.
		//	If multiple documents are provided, return a JSON array.
		//	""");

		//string[] documentTypes = [
		//	"W-2", "1099-INT", "1099-DIV", "1099-MISC", "1099-NEC", "1099-B",
		//	"1099-R", "1099-S", "1099-G", "1099-K", "W-4", "W-9", "1040",
		//	"1040-SR", "Schedule A", "Schedule B", "Schedule C", "Schedule D",
		//	"Schedule E", "Schedule SE"
		//];

		//foreach (var docType in documentTypes)
		//{
		//	sb.AppendLine($"""

		//		## {docType}
		//		When processing a {docType} form, extract all relevant fields including but not limited to:
		//		taxpayer identification numbers, names, addresses, filing status, income amounts,
		//		deduction amounts, tax amounts, withholding amounts, and any supplementary schedules.
		//		Validate that all monetary amounts are formatted as decimal numbers with two decimal places.
		//		Cross-reference fields for internal consistency and flag any discrepancies or missing
		//		required fields. If a field is present but illegible, set its value to null and add a
		//		warning in a separate "warnings" array. When the form includes multiple copies (e.g.
		//		Copy A, Copy B), only process the primary copy. Return results as a JSON object with
		//		the document_type set to "{docType}" and include a confidence_score between 0 and 1
		//		for each extracted field based on the clarity of the source.
		//		""");
		//}

		//return sb.ToString();
	}

	public GeminiCachedChatClientFacts()
	{
		var config = new ConfigurationBuilder()
			.AddUserSecrets("nc-hub")
			.AddEnvironmentVariables("nc_hub__")
			.Build()
			.GetSection("tests:nc_ai_tests:gemini");

		var services = new ServiceCollection()
			.Configure<AiContextCacheOptions>(config)
			.AddSingleton<IDistributedCache, MemoryDistributedCache>()
			.AddSingleton<IAiContextCache, AiContextCache>()
			.BuildServiceProvider();
		_cache = services.GetRequiredService<IAiContextCache>();

		_apiKey = config["apikey"]!;
		_model = config["model"]!;
		_genAiClient = new Client(apiKey: _apiKey);
		_client = new GeminiChatClient(_cache, _genAiClient, _model);
	}

	public async Task InitializeAsync()
	{
		await _client.CreateCacheAsync(SystemInstruction, ttl: "300s", displayName: "nc-ai-test");
	}

	public async Task DisposeAsync()
	{
		await _client.DeleteCacheAsync();
	}

	public class CreateCacheAsync : GeminiCachedChatClientFacts
	{
		[Fact]
		public void SetsCachedContentName()
		{
			Assert.NotNull(_client.CachedContentName);
			Assert.StartsWith("cachedContents/", _client.CachedContentName);
		}
	}

	public class GetResponseAsync : GeminiCachedChatClientFacts
	{
		[Fact]
		public async Task WithCachedPromptReturnsResult()
		{
			var message = new ChatMessage(ChatRole.User, "What document types can you extract?");
			var response = await _client.GetResponseAsync([message]);

			Assert.NotNull(response);
			Assert.False(string.IsNullOrWhiteSpace(response.Text));
		}

		[Fact]
		public async Task ReportsCachedTokenUsage()
		{
			var message = new ChatMessage(ChatRole.User, "What fields do you extract from a W-2?");
			var response = await _client.GetResponseAsync([message]);

			Assert.NotNull(response.Usage);
			Assert.True(response.Usage.InputTokenCount > 0);
			Assert.True(response.Usage.CachedInputTokenCount > 0);
		}

		[Fact]
		public async Task WithUriContentAnalyzesDocument()
		{
			var message = new ChatMessage(ChatRole.User, [
				new UriContent(
					"https://nascacht-io-sample.s3.us-east-1.amazonaws.com/financial/w2.pdf",
					"application/pdf"),
				new TextContent("Extract the fields per your instructions.")
			]);

			var response = await _client.GetResponseAsync([message]);

			Assert.NotNull(response);
			Assert.Contains("44", response.Text); // partial match on wages
		}

		[Fact]
		public async Task MultipleDocumentsReusesSameCache()
		{
			var cacheName = _client.CachedContentName;

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

				var response = await _client.GetResponseAsync([message]);
				Assert.NotNull(response);
				Assert.False(string.IsNullOrWhiteSpace(response.Text));
			}

			// Cache name should not have changed between calls
			Assert.Equal(cacheName, _client.CachedContentName);
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
				// var file = new UriContent(child.Value, "application/pdf");
				var file = new UriContent("https://storage.googleapis.com/nascacht-io-tests/bookmark.pdf", "application/pdf");
				// var question = new TextContent("I'm trying to figure out how to split up this PDF, if needed. Return a table of: Document Name, Document Type, Start Page, End Page. If this PDF is just 1 document, a table with 1 row is appropriate.");
				// var question = new TextContent("Summarize this document.");
				var userMessage = new ChatMessage(
					ChatRole.User,
					[file]
				);
				var response = await uploader.GetResponseAsync([userMessage]);
				Assert.NotNull(response.Text);
			}
		}
	}

	public class GetStreamingResponseAsync : GeminiCachedChatClientFacts
	{
		[Fact]
		public async Task WithCachedPromptStreamsTokens()
		{
			var message = new ChatMessage(ChatRole.User, "List the fields you extract.");
			var result = new StringBuilder();

			await foreach (var update in _client.GetStreamingResponseAsync([message]))
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
			// Upload a known file directly via the SDK (no cache, no wrapper)
			using var http = new HttpClient();
			var pdfUrl = "https://nascacht-io-sample.s3.us-east-1.amazonaws.com/financial/w2.pdf";
			var bytes = await http.GetByteArrayAsync(pdfUrl);

			var file = await _genAiClient.Files.UploadAsync(
				bytes, "w2.pdf",
				new UploadFileConfig { MimeType = "application/pdf" });

			// Wait for processing
			while (file.State == FileState.Processing)
			{
				await Task.Delay(1000);
				file = await _genAiClient.Files.GetAsync(file.Name!);
			}

			// Dump file properties for diagnostics
			var diag = $"Name={file.Name}, Uri={file.Uri}, State={file.State?.Value}, MimeType={file.MimeType}";

			Assert.Equal("ACTIVE", file.State?.Value);
			Assert.NotNull(file.Uri);

			// Try using the file directly via SDK — NO cached content
			var contents = new List<Content>
			{
				new()
				{
					Role = "user",
					Parts =
					[
						new Part
						{
							FileData = new FileData
							{
								FileUri = file.Uri,
								MimeType = "application/pdf"
							}
						},
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
		public async Task UploadAndUseFile_SdkDirect_WithCache()
		{
			// Same as above but WITH cached content
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

			var diag = $"Name={file.Name}, Uri={file.Uri}, State={file.State?.Value}, Cache={_client.CachedContentName}";

			var contents = new List<Content>
			{
				new()
				{
					Role = "user",
					Parts =
					[
						new Part
						{
							FileData = new FileData
							{
								FileUri = file.Uri,
								MimeType = "application/pdf"
							}
						},
						new Part { Text = "Extract the fields per your instructions." }
					]
				}
			};

			var config = new GenerateContentConfig
			{
				CachedContent = _client.CachedContentName
			};

			try
			{
				var response = await _genAiClient.Models.GenerateContentAsync(
					_model, contents, config);
				var text = response.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
				Assert.False(string.IsNullOrWhiteSpace(text), $"Empty response. {diag}");
			}
			catch (Exception ex)
			{
				Assert.Fail($"GenerateContent failed. {diag}. Error: {ex.Message}");
			}
		}
	}
}
