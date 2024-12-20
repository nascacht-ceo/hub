using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Test fixture for all Azure-based tests.
/// </summary>
public class AzureTestFixture: ITestFixture
{
    public ICloudFileManager Manager { get; private set; }
    public AzureTestFixture()
    {
        var config = new ConfigurationBuilder().AddJsonFile("azure.json").Build();
        var services = new ServiceCollection().AddAzure(config.GetSection("Azure")).BuildServiceProvider();
        Manager = services.GetRequiredService<ICloudFileManager>();
    }
}

/// <summary>
/// Collection definition for <see cref="AzureTestFixture">.
/// </summary>
[CollectionDefinition("AzureTestFixture")]
public class FixtureCollection : ICollectionFixture<AzureTestFixture> { }