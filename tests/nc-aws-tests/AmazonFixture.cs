using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nc.Extensions.DependencyInjection;
using System.Diagnostics;

namespace nc.Aws.Tests;

[CollectionDefinition(nameof(AmazonFixture))]
public class AmazonFixture : ICollectionFixture<AmazonFixture>, IAsyncLifetime
{
	private const int LocalStackPort = 4566;

	public IContainer? LocalStackContainer { get; private set; }
	public IConfiguration Configuration { get; private set; } = null!;
	public IServiceProvider Services { get; private set; } = null!;
	public string ServiceUrl { get; private set; } = null!;

	public AmazonFixture()
	{
		Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
	}

	public async Task InitializeAsync()
	{
		LocalStackContainer = new ContainerBuilder()
			.WithImage("localstack/localstack")
			.WithPortBinding(LocalStackPort, true)
			.WithEnvironment("SERVICES", "lambda,s3,secretsmanager,dynamodb")
			.WithEnvironment("LAMBDA_RUNTIME_ENVIRONMENT_TIMEOUT", "120")
			.WithEnvironment("DEBUG", "1")
			.WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r
				.ForPath("/_localstack/health")
				.ForPort(LocalStackPort)))
			.Build();

		await LocalStackContainer.StartAsync();
		ServiceUrl = $"http://{LocalStackContainer.Hostname}:{LocalStackContainer.GetMappedPublicPort(LocalStackPort)}/";

		Configuration = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["nc:aws:ServiceUrl"] = ServiceUrl
			})
			.Build()
			.GetSection("nc");

		Services = new ServiceCollection()
			.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Trace))
			.AddNascachtAwsServices(Configuration)
			.BuildServiceProvider();

		Debug.WriteLine($"LocalStack started at {ServiceUrl}");
	}

	public async Task DisposeAsync()
	{
		if (LocalStackContainer != null)
			await LocalStackContainer.StopAsync();
	}
}
