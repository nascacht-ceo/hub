using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using nc.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            .AddOpenApiService(configuration.GetSection(OpenApiServiceOptions.ConfigurtaionPath))
            .BuildServiceProvider();
        var options = services.GetService<IOptions<OpenApiServiceOptions>>();
        Assert.NotNull(options);
    }

    [Fact]
    public void RegistersOptionsViaDefault()
    {
        var services = new ServiceCollection()
            .AddOpenApiService()
            .BuildServiceProvider();
        var options = services.GetService<IOptions<OpenApiServiceOptions>>();
        Assert.NotNull(options);
    }

    [Fact]
    public void RegistersOptionsViaClass()
    {
        var options = new OpenApiServiceOptions();
        var services = new ServiceCollection()
            .AddOpenApiService(options)
            .BuildServiceProvider();
        Assert.NotNull(services.GetService<IOptions<OpenApiServiceOptions>>());
    }

    [Fact]
    public void LoadsSpecificationsFromConfig()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("facts.json")
            .Build();
        var services = new ServiceCollection()
            .AddOpenApiService(configuration.GetSection(OpenApiServiceOptions.ConfigurtaionPath))
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
}
