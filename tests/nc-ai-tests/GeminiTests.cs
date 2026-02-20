using Google.Cloud.AIPlatform.V1;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using nc.Ai.Gemini;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace nc.Ai.Tests;

public class GeminiTests : CommonTests
{
	public IConfiguration Configuration { get; }

	public GeminiTests()
	{
		Configuration = new ConfigurationBuilder()
			.AddUserSecrets("nc-hub")
			.AddEnvironmentVariables("nc_hub__")
			.Build();

		Client = new GeminiChatClient(new GeminiChatClientOptions
		{
			Model = "gemini-2.5-pro",
			ApiKey = Configuration["tests:nc_ai_tests:gemini:apikey"]
		});
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
