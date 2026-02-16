using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nc.Cloud;
using nc.Extensions.DependencyInjection;
using nc.Models;
using System.Diagnostics;

namespace nc.Aws.Tests;

[CollectionDefinition(nameof(AmazonFixture))]
public class AmazonFixture : ICollectionFixture<AmazonFixture>, IAsyncLifetime
{
	private const int LocalStackPort = 4566;
	private const string LocalStackTenantName = "localstack";

	public IContainer? LocalStackContainer { get; private set; }
	public IConfiguration Configuration { get; private set; } = null!;
	public IServiceProvider Services { get; private set; } = null!;
	public ITenantAccessor<AmazonTenant> TenantAccessor { get; private set; }
	public AmazonTenant Tenant { get; private set; }
	public string ServiceUrl { get; private set; } = null!;
	private IDisposable? _tenantScope;

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
			// .AddSingleton<IStore<AmazonTenant, string>>(tenantStore)
			.AddNascachtAmazonServices(Configuration)
			.BuildServiceProvider();

		TenantAccessor = Services.GetRequiredService<ITenantAccessor<AmazonTenant>>();
		Tenant = new AmazonTenant
		{
			TenantId = LocalStackTenantName,
			Name = LocalStackTenantName,
			AccessKey = "test",
			SecretKey = "test",
			ServiceUrl = ServiceUrl
		};
		_tenantScope = TenantAccessor.SetTenant(Tenant);

		Debug.WriteLine($"LocalStack started at {ServiceUrl}");
	}

	public async Task DisposeAsync()
	{
		_tenantScope?.Dispose();
		if (LocalStackContainer != null)
			await LocalStackContainer.StopAsync();
	}
}
