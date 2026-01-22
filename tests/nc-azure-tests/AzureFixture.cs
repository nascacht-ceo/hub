using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nc.Extensions;
using nc.Models;
using nc.Models.Tests;

/// <summary>
/// Test fixture for all Azure-based tests.
/// </summary>
public class AzureFixture: ITestFixture
{
	public readonly IConfigurationRoot Configuration;
	public readonly ServiceProvider Services;

	public ICloudFileManager Manager { get; private set; }
    public AzureFixture()
    {
        Configuration = new ConfigurationBuilder()
            // .AddJsonFile("artifacts/azure.json")
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("nc-hub")
            .AddEnvironmentVariables("nc_hub__")
			.Build();
        Services = new ServiceCollection()
            .AddLogging(lb => lb.AddInMemoryLogger())
            .AddNascachtAzureServices(Configuration.GetSection("nc"))
            .AddSingleton(typeof(IStore<,>), typeof(MockStore<,>))
			.BuildServiceProvider();
        Manager = Services.GetRequiredService<ICloudFileManager>();
    }
}

/// <summary>
/// Collection definition for <see cref="AzureFixture">.
/// </summary>
[CollectionDefinition(nameof(AzureFixture))]
public class FixtureCollection : ICollectionFixture<AzureFixture> { }
