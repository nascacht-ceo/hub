using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using nc.Extensions.DependencyInjection;
using System.Globalization;

namespace nc.OpenApi.Tests;

public class OpenApiServiceExtensionsFacts
{
    private readonly IConfigurationRoot _configuration;
    private readonly ServiceProvider _services;

    [Fact]
    public void RegistersOptionsViaConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("facts.json")
            .Build();
        var services = new ServiceCollection()
            .AddNascachtOpenApiService(configuration.GetSection("nc"))
            .BuildServiceProvider();
        var options = services.GetService<IOptions<OpenApiServiceOptions>>();
        Assert.NotNull(options);
    }

    [Fact]
    public void RegistersOptionsViaDefault()
    {
        var services = new ServiceCollection()
            .AddNascachtOpenApiService()
            .BuildServiceProvider();
        var options = services.GetService<IOptions<OpenApiServiceOptions>>();
        Assert.NotNull(options);
    }

    [Fact]
    public void RegistersOptionsViaClass()
    {
        var options = new OpenApiServiceOptions();
        var services = new ServiceCollection()
            .AddNascachtOpenApiService(options)
            .BuildServiceProvider();
        Assert.NotNull(services.GetService<IOptions<OpenApiServiceOptions>>());
    }

	[Theory]
    [InlineData("en-US", "List ")]
	[InlineData("es", "Lista ")]
	public void RegistersLocalizedResources(string culture, string value)
	{
		Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
		Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;
		var options = new OpenApiServiceOptions();
		var services = new ServiceCollection()
			.AddNascachtOpenApiService(options)
			.BuildServiceProvider();
        var localizer = services.GetService<IStringLocalizer<Resources.Documentation>>();

		Assert.NotNull(localizer);
        var message = localizer[nameof(Resources.Documentation.ListEndpoints)];
        Assert.NotEqual(nameof(Resources.Documentation.ListEndpoints), message);
        Assert.StartsWith(value, message);

		Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
		Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
	}

	[Fact]
    public void LoadsSpecificationsFromConfig()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("facts.json")
            .Build();
        var services = new ServiceCollection()
            .AddNascachtOpenApiService(configuration.GetSection("nc"))
            .BuildServiceProvider();
        var options = services.GetService<IOptions<OpenApiServiceOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options.Value);
        var openApiOptions = options.Value;
        Assert.NotNull(openApiOptions.Specifications);
        Assert.NotEmpty(openApiOptions.Specifications);
        Assert.Contains("local-config", openApiOptions.Specifications.Keys);
        Assert.DoesNotContain("PetStore", openApiOptions.Specifications.Keys);
    }

    [Fact(Skip ="refactoring required")]
    public async Task AddsTransformer()
    {
        var services = new ServiceCollection()
            .ConfigureOptions<SampleOptions>()
			.AddOpenApi()
            .BuildServiceProvider();

        // var doc = services.GetRequiredService<Microsoft.AspNetCore.OpenApi.IOpenApiDocumentProvider>();
	}
    public class TestProgram: Program
    { }

    public class TestFactory: WebApplicationFactory<TestProgram>
    {
		protected override IWebHostBuilder? CreateWebHostBuilder()
		{
			return base.CreateWebHostBuilder();
		}
		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			builder.UseContentRoot(Directory.GetCurrentDirectory());

			builder.ConfigureServices(services =>
			{
				services.ConfigureOptions<SampleOptions>();
				services.AddEndpointsApiExplorer();
				services.AddOpenApi();
			});
			builder.Configure(app =>
			{
				app.UseEndpoints(endpoints =>
				{
					endpoints.MapOpenApi();
				});
			});
		}
	}
	public class SampleOptions : IConfigureOptions<OpenApiOptions>
	{
        public static bool Configured = false;
		public void Configure(OpenApiOptions options)
		{
            options.AddDocumentTransformer((document, context, token) =>
            {
                Configured = true;
                return Task.CompletedTask;
            });
		}
	}
}
