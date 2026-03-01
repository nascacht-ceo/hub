using Google.Cloud.AIPlatform.V1;
using FunctionCallingConfigMode = Google.GenAI.Types.FunctionCallingConfigMode;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nc.Ai.Gemini;
using nc.Ai.Interfaces;
using System.Text.Json;

namespace nc.Ai.Tests;

public class GeminiTests : CommonTests, IAsyncLifetime
{
	private readonly ServiceProvider _services;

	public GeminiTests()
	{
		var configuration = new ConfigurationBuilder()
			.AddUserSecrets("nc-hub")
			.AddEnvironmentVariables("nc_hub__")
			.Build();

		_services = new ServiceCollection()
			.AddLogging()
			.AddUsageTracking()
			.AddAiGemini("default", opts =>
			{
				opts.Model = "gemini-2.5-pro";
				opts.FunctionCallingMode = FunctionCallingConfigMode.Any;
				configuration.GetSection("tests:nc_ai_tests:gemini").Bind(opts);
			})
			.AddAiGemini("split", opts =>
			{
				opts.Model = "gemini-2.5-pro";
				opts.Timeout = TimeSpan.FromMinutes(5);
				opts.RetryCount = 2;
				opts.Instructions = """
					You are a helpful assistant for processing PDF documents. When given a PDF that contains multiple individual documents combined into one file, you analyze the document and split it into its constituent documents. You return a JSON array where each element represents one document with these fields:
					- Title: the document title or type
					- Template: the "kind" of document based on its layout and content, e.g. "invoice", "contract", "w2", "1099", "deed", etc.
					- StartPage: the 1-based page number where this document begins
					- EndPage: the 1-based page number where this document ends
					- RecordingDate: the recording date if present (ISO 8601 date), omit if not found
					- NotarizedDate: the notarization date if present (ISO 8601 date), omit if not found
					- SignatureDate: the signature date if present (ISO 8601 date), omit if not found
					- Metadata: an object containing any key-value pairs you can extract, where the key is the name of the field (PascalCase, alphanumeric only) and the value is the extracted value. For example, for a W2 you might extract {"Employer": "Contoso", "Wages": "44,629.35", "TaxWithheld": "5,000"}.
					Return only the JSON array, no markdown or other text.
					""";
				configuration.GetSection("tests:nc_ai_tests:gemini").Bind(opts);
			})
			.BuildServiceProvider();
	}

	public Task InitializeAsync()
	{
		Client = _services.GetRequiredService<IAgentManager>().GetChatClient("default");
		return Task.CompletedTask;
	}

	public async Task DisposeAsync() => await _services.DisposeAsync();

	[SkippableFact(Skip="ad-hoc testing")]
	public async Task Split()
	{
		var pdfPath = Path.Combine(AppContext.BaseDirectory, "data", "split.sample.pdf");
		Skip.If(!File.Exists(pdfPath));

		var client = _services.GetRequiredService<IAgentManager>().GetChatClient("split");
		var bytes = await File.ReadAllBytesAsync(pdfPath);

		var pdf = new DataContent(bytes, "application/pdf");
		var message = new ChatMessage(ChatRole.User, [pdf]);
		var options = new ChatOptions { ResponseFormat = ChatResponseFormat.Json };
		var response = await client.GetResponseAsync([message], options);

		var docs = response.Deserialize<JsonElement[]>();
		Assert.NotNull(docs);
		Assert.NotEmpty(docs);
		foreach (var doc in docs)
		{
			Assert.True(doc.TryGetProperty("Title", out _), $"Missing Title in: {doc}");
			Assert.True(doc.TryGetProperty("StartPage", out _), $"Missing StartPage in: {doc}");
			Assert.True(doc.TryGetProperty("EndPage", out _), $"Missing EndPage in: {doc}");
		}
	}

	[Fact]
	public async Task GetsModels()
	{
		var clientBuilder = new ModelServiceClientBuilder
		{
			Endpoint = $"https://us-central1-aiplatform.googleapis.com/"
		};
		ModelServiceClient client = await clientBuilder.BuildAsync();

		string parent = $"projects/seventh-seeker-476512-r1/locations/us-central1";
		ListModelsRequest request = new ListModelsRequest { Parent = parent };

		try
		{
			Console.WriteLine($"--- Models in us-central1 ---");
			var models = client.ListModels(request);

			foreach (var model in models)
			{
				Console.WriteLine($"Name: {model.Name}");
				Console.WriteLine($"Display Name: {model.DisplayName}");
				Console.WriteLine($"Supported Prediction: {model.SupportedExportFormats}");
				Console.WriteLine("-------------------------");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error fetching models: {ex.Message}");
		}
	}

	[Fact(Skip = "local testing only.")]
	public void EnvironmentVariablesTranslated()
	{
		Environment.SetEnvironmentVariable("nc_hub__tests__compound__key", "value");
		Environment.SetEnvironmentVariable("NC_HUB__TESTS__NC_AI_TESTS__GEMINI__APIKEY", "dummy");
		var config = new ConfigurationBuilder().AddEnvironmentVariables("nc_hub__").Build();
		var section = config.GetSection("tests");
		Assert.Equal("value", section["compound:key"]);
		Assert.Equal("dummy", section["nc_ai_tests:gemini:apikey"]);
	}
}
