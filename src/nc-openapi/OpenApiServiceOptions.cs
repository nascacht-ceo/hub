using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace nc.OpenApi;

/// <summary>
/// Options class for configuring OpenAPI specifications in the application.
/// </summary>
public class OpenApiServiceOptions : IValidatableObject
{
    public const string ConfigurtaionPath = "nc:openapi";
    

    private IDictionary<string, OpenApiSpecification> _specifications 
        = new Dictionary<string, OpenApiSpecification>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the collection of OpenAPI specifications.
    /// </summary>
    public IDictionary<string, OpenApiSpecification> Specifications 
    {
        get
        {
            if (_specifications.Count == 0)
                _specifications.Add("Petstore", new OpenApiSpecification("https://petstore3.swagger.io/api/v3/openapi.json"));
            return _specifications;
        }
        set {  _specifications = value; } 
    } 

    public string HttpClientName { get; set; } = "OpenApiServiceClient";

    public string CacheKey { get; set; } = "OpenApiServiceCache";

    public int CacheMinutes { get; set; } = 5;

    private DistributedCacheEntryOptions? _cacheOptions;
    public DistributedCacheEntryOptions CacheOptions 
    { 
        get
        {
            _cacheOptions ??= new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheMinutes) };
            return _cacheOptions;
        }
        set
        {
            _cacheOptions = value ?? throw new ArgumentNullException(nameof(value), "Cache options cannot be null.");
        }
    } 

    public OpenApiServiceOptions()
    {
        Specifications ??= new Dictionary<string, OpenApiSpecification>(StringComparer.OrdinalIgnoreCase);
    }


    /// <summary>
    /// Validates the current object and its associated OpenAPI specifications.
    /// </summary>
    /// <remarks>This method performs the following validations: 
    /// <list type="bullet"> 
    /// <item>
    /// <description>Ensures that at least one OpenAPI specification is provided.</description> 
    /// </item> 
    /// <item> 
    /// <description>Delegates validation of individual specifications to their own <c>Validate</c> method.</description> 
    /// </item> 
    /// </list> 
    /// If any validation errors are found, they are returned as <seecref="ValidationResult"/> 
    /// objects, with detailed error messages and member names indicating the source of the error.
    /// </remarks>
    /// <param name="validationContext">The context in which the validation is performed, providing additional information such as service containers or items.</param>
    /// <returns>A collection of <see cref="ValidationResult"/> objects that describe any validation errors.  
    /// The collection will be empty if the object is valid.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (Specifications == null || !Specifications.Any())
        {
            results.Add(new ValidationResult("At least one OpenAPI specification must be provided.", new[] { nameof(Specifications) }));
            return results;
        }

        foreach (var specification in Specifications)
        {
            // Check for null spec
            if (specification.Value is null)
            {
                results.Add(new ValidationResult($"Specification {specification.Key} is null."));
                continue;
            }

            // Delegate to child validator
            var context = new ValidationContext(specification, validationContext, validationContext.Items);
            foreach (var validationResult in specification.Value.Validate(context))
            {
                results.Add(new ValidationResult(validationResult.ErrorMessage));
            }
        }
        return results;
    }
}

/// <summary>
/// Represents an OpenAPI specification.
/// </summary>

public class OpenApiSpecification : IValidatableObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiSpecification"/> class.
    /// </summary>
    /// <remarks>This constructor creates a default instance of the <see cref="OpenApiSpecification"/> class.
    /// Use this to initialize a new OpenAPI specification object with default settings.</remarks>
    public OpenApiSpecification()
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiSpecification"/> class with the specified OpenAPI
    /// specification URL.
    /// </summary>
    /// <param name="specUrl">The URL of the OpenAPI specification. This must be a valid, non-null, and non-empty URL pointing to the OpenAPI
    /// document.</param>
    public OpenApiSpecification(string specUrl)
    {
        SpecUrl = specUrl;
    }

    /// <summary>
    /// URL of the OpenAPI specification.
    /// </summary>
    public string? SpecUrl { get; set; }

    private string? _baseUrl;
    /// <summary>
    /// Gets or sets the base URL used for API requests.
    /// If this can be inferred from the SpecUrl, it will be set automatically.
    /// </summary>
    public string? BaseUrl
    {
        get => string.IsNullOrEmpty(_baseUrl) ? SpecUrl : _baseUrl;
        set => _baseUrl = value;
    }

    /// <summary>
    /// Authentication scheme to use for the API.
    /// </summary>
    public string? AuthenticationScheme { get; set; }

    /// <summary>
    /// Validates the current object's properties and returns a collection of validation results.
    /// </summary>
    /// <remarks>This method checks the following conditions: <list type="bullet"> <item> <description>The
    /// <see cref="SpecUrl"/> property must not be null, empty, or whitespace, and it must be a valid absolute
    /// URI.</description> </item> <item> <description>If the <see cref="BaseUrl"/> property is provided, it must be a
    /// valid absolute URI.</description> </item> </list> If any of these conditions are not met, a corresponding <see
    /// cref="ValidationResult"/> is added to the returned collection.</remarks>
    /// <param name="validationContext">The context in which the validation is performed. This parameter provides additional information about the
    /// validation operation.</param>
    /// <returns>A collection of <see cref="ValidationResult"/> objects that describe any validation errors.  The collection will
    /// be empty if the object is valid.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (string.IsNullOrWhiteSpace(SpecUrl))
        {
            results.Add(new ValidationResult("The SpecUrl property is required.", new[] { nameof(SpecUrl) }));
        }
        else if (!Uri.IsWellFormedUriString(SpecUrl, UriKind.Absolute))
        {
            results.Add(new ValidationResult($"The SpecUrl must be a valid absolute URI. {SpecUrl} is invalid.", new[] { nameof(SpecUrl) }));
        }

        if (!string.IsNullOrWhiteSpace(_baseUrl) &&
            !Uri.IsWellFormedUriString(BaseUrl, UriKind.Absolute))
        {
            results.Add(new ValidationResult($"The BaseUrl must be a valid absolute URI if provided. {BaseUrl} is invalid.", new[] { nameof(BaseUrl) }));
        }

        return results;
    }

    public static implicit operator OpenApiSpecification(string url)
    {
        return new OpenApiSpecification(url);
    }

    public static implicit operator OpenApiSpecification(Uri uri)
    {
        return new OpenApiSpecification(uri.AbsoluteUri);
    }

}
