using Google.Cloud.AIPlatform.V1;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nc.Ai.Gemini;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace nc.Ai.Tests;

public class Gemini : CommonTests
{
	private readonly IAiContextCache _cache;

	public IConfiguration Configuration { get; }
	// public GeminiClientOptions Options { get; }

	public Gemini()
	{
		Configuration = new ConfigurationBuilder()
			.AddUserSecrets("nc-hub")
			.AddEnvironmentVariables("nc_hub__")
			.Build();
			// .GetSection("tests:nc_ai_tests:gemini");

		var services = new ServiceCollection()
			.Configure<AiContextCacheOptions>(Configuration.GetSection("tests:nc_ai_tests"))
			.AddSingleton<IDistributedCache, MemoryDistributedCache>()
			.AddSingleton<IAiContextCache, AiContextCache>()
			.BuildServiceProvider();
		_cache = services.GetRequiredService<IAiContextCache>();

		//Options = new GeminiClientOptions { ApiKey = Configuration["apikey"]!, ModelId = Configuration["model"] };
		//Options.ModelId = "gemini-2.5-pro"; // "gemini -1.5-pro-latest";
		Client = new GeminiChatClient(_cache, "gemini-2.5-pro", apiKey: Configuration["tests:nc_ai_tests:gemini:apikey"]);

		//Client = new ChatClient(Configuration["model"], Configuration["secretkey"])
		//	.AsIChatClient();
	}

	//[Fact]
	//public async Task UploadsFiles()
	//{
	//	var fileClient = new GeminiDotnet.GeminiFileClient(apiKey);
	//	using var stream = await s3Client.GetObjectStreamAsync(...); // or local stream
	//	var upload = await fileClient.UploadFileAsync(stream, new UploadFileOptions { MimeType = "application/pdf" });

	//	// 2. Reference the uploaded file's URI (stored for 48 hours)
	//	var userMessage = new ChatMessage(
	//		ChatRole.User,
	//		[new FileContent(upload.Uri, "application/pdf"), question]
	//	);
	//}

	[Fact]
	public async Task GetsModels()
	{
		var clientBuilder = new ModelServiceClientBuilder
		{
			Endpoint = $"https://us-central1-aiplatform.googleapis.com/"
		};
		ModelServiceClient client = await clientBuilder.BuildAsync();

		// 2. Prepare the request
		string parent = $"projects/seventh-seeker-476512-r1/locations/us-central1";
		ListModelsRequest request = new ListModelsRequest { Parent = parent };

		try
		{
			// 3. List the models
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
	//[Fact]
	//public async Task Sample()
	//{
	//	var response = await Client.GetResponseAsync("What is AI?");
	//	Assert.NotNull(response);
	//}

	//[Fact]
	//public async Task FunctionCalling()
	//{
	//	ChatOptions chatOptions = new()
	//	{
	//		Tools = [AIFunctionFactory.Create(Functions.GetWeather)],

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
	//	var answer = response.ToString();
	//	Assert.True(answer.Contains("sunny") || answer.Contains("raining"));
	//}

	//[Fact]

	//public async Task Embedding()
	//{
	//	var options = new GeminiClientOptions
	//	{
	//		ApiKey = Configuration["apikey"]!,
	//		ModelId = Configuration["embeddingmodel"]!
	//	};
	//	IEmbeddingGenerator<string, Embedding<float>> generator =
	//		new GeminiEmbeddingGenerator(options);

	//	var embeddings = await generator.GenerateAsync("What is AI?");
	//	Assert.NotNull(embeddings);
	//	var vectors = string.Join(", ", embeddings.Vector.ToArray());
	//	Assert.NotEmpty(vectors);
	//}

	//[Fact]
	//public async Task FileAnalysis()
	//{
	//	var file = new UriContent("https://nascacht-io-sample.s3.us-east-1.amazonaws.com/financial/w2.pdf", "application/pdf");
	//	var question = new TextContent("What is the total amount in box 1?");
	//	var userMessage = new ChatMessage(
	//		ChatRole.User,
	//		[file, question] // Combining text and file content
	//	);
	//	var response = await Client.GetResponseAsync([userMessage]);
	//	Assert.Contains("44,629.35", response.Text);
	//}
}
