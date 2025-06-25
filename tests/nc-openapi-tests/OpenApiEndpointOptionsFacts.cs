using nc.OpenApi;
using System.ComponentModel.DataAnnotations;

namespace nc.OpenApi.Tests;

public partial class OpenApiEndpointOptionsFacts
{
    [Fact]
    public void InitializesSpecifications()
    {
        var options = new OpenApiServiceOptions();
        Assert.NotNull(options.Specifications);
        Assert.NotEmpty(options.Specifications);
    }

    [Fact]
    public void ValidatesSpecificationUrl()
    {
        var options = new OpenApiServiceOptions();
        options.Specifications.Add("Good", new OpenApiSpecification("https://example.com/openapi.json"));
        options.Specifications.Add("Bad", new OpenApiSpecification("example.com/openapi.json"));
        options.Specifications.Add("Other", new OpenApiSpecification("sftp://example.com/openapi.json"));

        var results = new List<ValidationResult>();
        var context = new ValidationContext(options);
        bool isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);
        Assert.False(isValid);
        Assert.Single(results);
    }
}
