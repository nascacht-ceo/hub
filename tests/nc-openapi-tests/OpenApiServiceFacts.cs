using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.DependencyInjection;
using nc.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.OpenApi.Tests;

public class OpenApiServiceFacts
{
    private readonly ServiceProvider _services;

    public OpenApiServiceFacts()
    {
        _services = new ServiceCollection().AddOpenApiService().BuildServiceProvider();
    }

    [Fact]
    public void ListEndpoints_ReturnsSpecifications()
    {
        // Arrange
        var openApiService = _services.GetRequiredService<OpenApiService>();
        openApiService.ListEndpoints();
        Assert.Contains("petstore", openApiService.ListEndpoints());
    }

    [Fact]
    public async Task Fetches_Specification()
    {
        // Arrange
        var openApiService = _services.GetRequiredService<OpenApiService>();
        var spec = await openApiService.GetSpecificationAsync("petstore");
        Assert.NotNull(spec);
    }

    [Fact]
    public async Task Proxies_Request()
    {
        var openApiService = _services.GetRequiredService<OpenApiService>();

        // Create a mock HttpRequest using DefaultHttpContext
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/petstore/pet/findByStatus?status=available";
        context.Request.Body = new MemoryStream(); // Empty body for GET

        var result = await openApiService.ProxyRequestAsync(context.Request);

        Assert.NotNull(result);

        var response = result as Ok<HttpResponseMessage>;
        using var reader = new StreamReader(response.Value.Content.ReadAsStream());
        var json = await reader.ReadToEndAsync();
        Assert.NotEmpty(json);
    }
}
