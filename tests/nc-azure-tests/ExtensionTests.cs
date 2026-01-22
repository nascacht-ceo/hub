using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public class ExtensionTests
{
    public class AddAzure : ExtensionTests
    {
        [Fact]
        public void AddsAzureServiceOptions()
        {
            var services = new ServiceCollection().AddAzure(new AzureServiceOptions()).BuildServiceProvider();
            var options = services.GetRequiredService<IOptions<AzureServiceOptions>>();
            Assert.NotNull(options);
        }

		[Fact(Skip = "work in progress")]
		public void AddsAzureServiceOptionsFromConfig()
        {
            var config = new ConfigurationBuilder().AddUserSecrets("nc-hub").AddEnvironmentVariables("nc_hub__").Build();
            var services = new ServiceCollection().AddNascachtAzureServices(config.GetSection("Azure")).BuildServiceProvider();
            var options = services.GetRequiredService<IOptions<AzureServiceOptions>>();
            Assert.NotNull(options);
        }

		[Fact(Skip = "work in progress")]
		public void AddsConfigureManagerOptions()
        {
            var config = new ConfigurationBuilder().AddUserSecrets("nc-hub").AddEnvironmentVariables("nc_hub__").Build();
            var services = new ServiceCollection().AddNascachtAzureServices(config.GetSection("Azure")).BuildServiceProvider();
            var options = services.GetRequiredService<IOptions<CloudFileManagerOptions>>();
            Assert.NotNull(options);
            Assert.NotEmpty(options.Value.ServiceFactories);
            Assert.Equal(2, options.Value.ServiceFactories.Count);
        }
    }
}