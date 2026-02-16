using Amazon.S3.Model;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using nc.Ai.Gemini;
using System.Text;

namespace nc.Ai.Tests;

public class GeminiCachedChatClientFacts : IAsyncLifetime
{
	private readonly GeminiCachedChatClient _client;
	private readonly string _apiKey;

	/// <summary>
	/// Gemini requires at least 2,048 tokens for cached content.
	/// This prompt is generated to exceed that minimum.
	/// </summary>
	private static readonly string SystemInstruction = BuildSystemInstruction();

	private static string BuildSystemInstruction()
	{
		var sb = new StringBuilder();
		sb.AppendLine("""
			You are a financial document analyst. For every document provided, extract
			the relevant fields and return them as JSON.
			Always respond with valid JSON only, no markdown fences.
			If multiple documents are provided, return a JSON array.
			""");

		string[] documentTypes = [
			"W-2", "1099-INT", "1099-DIV", "1099-MISC", "1099-NEC", "1099-B",
			"1099-R", "1099-S", "1099-G", "1099-K", "W-4", "W-9", "1040",
			"1040-SR", "Schedule A", "Schedule B", "Schedule C", "Schedule D",
			"Schedule E", "Schedule SE"
		];

		foreach (var docType in documentTypes)
		{
			sb.AppendLine($"""

				## {docType}
				When processing a {docType} form, extract all relevant fields including but not limited to:
				taxpayer identification numbers, names, addresses, filing status, income amounts,
				deduction amounts, tax amounts, withholding amounts, and any supplementary schedules.
				Validate that all monetary amounts are formatted as decimal numbers with two decimal places.
				Cross-reference fields for internal consistency and flag any discrepancies or missing
				required fields. If a field is present but illegible, set its value to null and add a
				warning in a separate "warnings" array. When the form includes multiple copies (e.g.
				Copy A, Copy B), only process the primary copy. Return results as a JSON object with
				the document_type set to "{docType}" and include a confidence_score between 0 and 1
				for each extracted field based on the clarity of the source.
				""");
		}

		return sb.ToString();
	}

	public GeminiCachedChatClientFacts()
	{
		var config = new ConfigurationBuilder()
			.AddUserSecrets("nc-hub")
			.AddEnvironmentVariables("nc_hub__")
			.Build()
			.GetSection("tests:nc_ai_tests:gemini");

		_apiKey = config["apikey"]!;
		var model = config["model"]!;
		_client = new GeminiCachedChatClient(new HttpClient(), _apiKey, model);
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
			Assert.NotNull(response.Usage.AdditionalCounts);
			Assert.True(response.Usage.AdditionalCounts.ContainsKey("cachedContentTokenCount"));
			Assert.True(response.Usage.AdditionalCounts["cachedContentTokenCount"] > 0);
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
			var config = new ConfigurationBuilder().AddUserSecrets("nc-hub").Build();
			foreach (var child in config.GetSection("sample:documents").GetChildren())
			{
				if (child.Value == null)
					continue;
				var file = new UriContent(child.Value, "application/pdf");
				var question = new TextContent("This PDF comprises multiple PDFs. Return a table of: Document Name, Document Type, Start Page, End Page, Recoring, Notarized, and Signed'.");
				var userMessage = new ChatMessage(
					ChatRole.User,
					[file, question] // Combining text and file content
				);
				var response = await _client.GetResponseAsync([userMessage]);
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

}
