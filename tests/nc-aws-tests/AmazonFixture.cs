using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nc.Extensions.DependencyInjection;

namespace nc.Aws.Tests;

[CollectionDefinition("Amazon")]
public class AmazonFixture : ICollectionFixture<AmazonFixture>
{
	public IConfiguration Configuration { get; }

	public IServiceProvider Services { get; }
	public AmazonFixture()
	{
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

}
