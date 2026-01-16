using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nc.Extensions.DependencyInjection;
using System.Diagnostics;

namespace nc.Aws.Tests;

[CollectionDefinition(nameof(AmazonFixture))]
public class AmazonFixture : ICollectionFixture<AmazonFixture>, IAsyncLifetime
{
	public IConfiguration Configuration { get; }

	public IServiceProvider Services { get; }


	public AmazonFixture()
	{
		Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

		Configuration = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.Build()
			.GetSection("nc");
;

		Services = new ServiceCollection()
			.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Trace))
			.AddNascachtAwsServices(Configuration)
			.BuildServiceProvider();
	}


	public Guid Id { get; } = Guid.NewGuid();
	public int InitCount { get; private set; }

	private bool _initialized = false;
	private readonly SemaphoreSlim _lock = new(1, 1);

	public async Task InitializeAsync()
	{
		if (_initialized) return;
		await _lock.WaitAsync();
		try
		{
			var timeout = TimeSpan.FromSeconds(5);
			var stopwatch = Stopwatch.StartNew();
			var httpClient = new HttpClient();
			InitCount++;
			Debug.WriteLine($"Fixture {Id} initialized {InitCount} times");
			while (stopwatch.Elapsed < timeout)
			{
				try
				{
					// LocalStack 2.0+ uses /_localstack/health
					var response = await httpClient.GetAsync("http://localhost:4566/_localstack/health");
					if (response.IsSuccessStatusCode)
					{
						_initialized = true;
						return; // LocalStack is up and services are healthy
					}
				}
				catch
				{
					// Port not open yet or connection refused
				}

				await Task.Delay(500);
			}
			_initialized = true;
			throw new Exception("The AmazonFixture requires LocalStack to be running on port 4566. Failed to connect within 5 seconds.");

		}
		finally
		{
			_lock.Release();
		}
	}

	public Task DisposeAsync()
		=> Task.CompletedTask;
}
