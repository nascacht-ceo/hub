using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace nc.OpenApi;

/// <summary>
/// Handles operations related to OpenAPI specifications and proxying requests to OpenAPI endpoints.
/// </summary>
/// <remarks>This class provides functionality to list available OpenAPI endpoints, retrieve OpenAPI
/// specifications, and proxy requests to configured OpenAPI endpoints. It validates the provided configuration options
/// and ensures proper handling of HTTP requests and responses.</remarks>
public class OpenApiService
{
    private OpenApiServiceOptions _options;
    private readonly IDistributedCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IStringLocalizer<Resources.Errors> _localizer;
    private readonly ILogger<OpenApiService>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiService"/> class, which handles interactions with OpenAPI
    /// endpoints.
    /// </summary>
    /// <remarks>This constructor configures the handler with the specified options, environment, and HTTP
    /// client.  The <paramref name="logger"/> parameter is optional and can be null if logging is not
    /// required.</remarks>
    /// <param name="options">The monitor for retrieving and tracking changes to <see cref="OpenApiServiceOptions"/>.</param>
    /// <param name="cache">The <see cref="IDistributedCache"/> instance used for caching OpenAPI specifications.</param>
    /// <param name="httpClientFactory">The <see cref="HttpClient"/> instance used to send HTTP requests to OpenAPI endpoints.</param>
    /// <param name="logger">An optional <see cref="ILogger{TCategoryName}"/> instance for logging diagnostic and operational information.</param>
    public OpenApiService(
        IOptionsMonitor<OpenApiServiceOptions> options,
        IHttpClientFactory httpClientFactory,
        IStringLocalizer<Resources.Errors> localizer,
        IDistributedCache? cache = null,
        ILogger<OpenApiService>? logger = null)
    {
        _options = SetOptions(options);
        _cache = cache ?? throw new ArgumentNullException(nameof(cache), "Distributed cache cannot be null");
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory), "HttpClient cannot be null");
        _localizer = localizer;
        _logger = logger;
    }

    /// <summary>
    /// Validates and retrieves the current <see cref="OpenApiServiceOptions"/> from the provided options monitor.
    /// </summary>
    /// <remarks>This method performs validation on the current <see cref="OpenApiServiceOptions"/> instance
    /// using data annotations. If validation fails, an <see cref="OptionsValidationException"/> is logged, and the
    /// original options are returned.</remarks>
    /// <param name="optionsMonitor">An <see cref="IOptionsMonitor{TOptions}"/> instance used to access the current <see
    /// cref="OpenApiServiceOptions"/>.</param>
    /// <returns>The validated <see cref="OpenApiServiceOptions"/> instance. If validation fails, the previously configured
    /// options are returned.</returns>
    private OpenApiServiceOptions SetOptions(IOptionsMonitor<OpenApiServiceOptions> optionsMonitor)
    {
        var options = optionsMonitor.CurrentValue;

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(options);

        bool isValid = Validator.TryValidateObject(
            options,
            context,
            validationResults,
            validateAllProperties: true
        );

        if (!isValid)
        {
            var exception = new OptionsValidationException(
                nameof(OpenApiServiceOptions),
                typeof(OpenApiServiceOptions),
                validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error")
            );
            _logger?.LogError(exception, "OpenApiEndpointOptions validation failed: {ValidationException}", string.Join(", ", validationResults.Select(r => r.ErrorMessage)));
            return _options; // Return the original options if validation fails
        }
        return options;
    }

    /// <summary>
    /// Retrieves a list of all available endpoint names.
    /// </summary>
    /// <remarks>This method returns the keys from the endpoint specifications defined in the application
    /// options.</remarks>
    /// <returns>A JSON-formatted response containing the names of all endpoints.  The response will be an empty collection if no
    /// endpoints are defined.</returns>
    public IEnumerable<string> ListEndpoints()
        => _options.Specifications.Keys.Select(k => k.ToLower());

    private static JsonSerializerOptions _serializerOptions = new JsonSerializerOptions()
    {
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve,
    };

    /// <summary>
    /// Retrieves the OpenAPI specification for the specified endpoint.
    /// </summary>
    /// <remarks>This method fetches the OpenAPI specification from a pre-configured location. If the
    /// specification URL is invalid  or the remote fetch fails, an appropriate error result is returned. Ensure that
    /// the endpoint is registered in the  configuration and that the URL is well-formed and accessible.</remarks>
    /// <param name="name">The name of the endpoint for which the OpenAPI specification is requested.</param>
    /// <returns>An <see cref="IResult"/> containing the OpenAPI specification as JSON if found and valid;  otherwise, a result
    /// indicating the error, such as <see cref="Results.NotFound"/> or <see cref="Results.Problem"/>.</returns>
    public async Task<OpenApiDocument> GetSpecificationAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!_options.Specifications.ContainsKey(name))
            throw new ArgumentOutOfRangeException(nameof(name), name,
                _localizer[nameof(Resources.Errors.OpenApiSpecNotRegistered), string.Join(", ", _options.Specifications.Keys)]);

        var json = await _cache.GetStringAsync($"{_options.CacheKey}:{name}", cancellationToken);
        if (json != null)
            return JsonToOpenApiDocument(json);

        var specification = _options.Specifications[name];
        var location = specification.SpecUrl;
        using var client = _httpClientFactory.CreateClient(_options.HttpClientName);
        try
        {
            var stream = await client.GetStreamAsync(location);
            var reader = new OpenApiStreamReader();
            var document = reader.Read(stream, out var diagnostic);

            if (diagnostic.Errors.Count > 0)
            {
                foreach (var error in diagnostic.Errors)
                    _logger?.LogWarning(error.Message);

                throw new InvalidOperationException(_localizer[nameof(Resources.Errors.OpenApiSpecNotRegistered), specification.SpecUrl!]);
            }
            await _cache.SetStringAsync(
                $"{_options.CacheKey}:{name}", 
                OpenApiDocumentToJson(document), 
                _options.CacheOptions, 
                cancellationToken);
            return document;
        }
        catch (HttpRequestException)
        {
            throw;
        }
    }

    private string OpenApiDocumentToJson(OpenApiDocument document, OpenApiSpecVersion version = OpenApiSpecVersion.OpenApi3_0)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        var openApiWriter = new OpenApiJsonWriter(writer);

        document.Serialize(openApiWriter, version);
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private OpenApiDocument JsonToOpenApiDocument(string json)
    {
        var openApiReader = new OpenApiStringReader();
        var document = openApiReader.Read(json, out var diagnostic);
        return document;
    }

    /// <summary>
    /// Proxies an incoming HTTP request to a target endpoint based on the specified path.
    /// </summary>
    /// <remarks>This method identifies the target endpoint by matching the provided <paramref name="path"/>
    /// against a set of predefined OpenAPI specifications. The request is then forwarded to the corresponding base URL
    /// with the appropriate relative path.  If the request body is present, it is included in the proxied request. The
    /// response from the target endpoint is streamed back to the caller, preserving the content type of the original
    /// response.</remarks>
    /// <param name="request">The incoming HTTP request to be proxied.</param>
    /// <returns>An <see cref="IResult"/> representing the proxied response. Returns a 404 Not Found result if no matching
    /// endpoint is found, or a 500 Internal Server Error result if the target endpoint's base URL is invalid.</returns>
    public async Task<IResult> ProxyRequestAsync(HttpRequest request)
    {
        string path = request.Path.Value;
        var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Ensure there's at least one segment for the endpoint name
        if (pathSegments.Length == 0)
        {
            return Results.NotFound("Invalid request path. Missing endpoint name.");
        }

        var name = pathSegments[0];

        // Retrieve the OpenApiEndpointOptions directly to access BaseUrl (which should be the root API server URL)
        if (!_options.Specifications!.TryGetValue(name, out var specification))
        {
            return Results.NotFound($"No OpenAPI endpoint registered for '{name}'");
        }

        // Validate BaseUrl from the specification configuration (this is the root server URL)
        if (string.IsNullOrEmpty(specification.BaseUrl) || !Uri.TryCreate(specification.BaseUrl, UriKind.Absolute, out var rootApiBaseUri))
        {
            _logger?.LogError("Invalid or missing BaseUrl for endpoint '{EndpointName}': '{BaseUrl}'", name, specification.BaseUrl);
            return Results.Problem($"Invalid or missing base URL for endpoint '{name}' in configuration.");
        }

        // Fetch the OpenAPI document to get the server URL (e.g., /api/v3)
        OpenApiDocument spec;
        try
        {
            spec = await this.GetSpecificationAsync(name, request.HttpContext.RequestAborted);
        }
        catch (Exception ex) // Catching generic Exception for robustness in proxying
        {
            _logger?.LogError(ex, "Failed to retrieve OpenAPI specification for endpoint '{EndpointName}' during proxying.", name);
            return Results.Problem($"Failed to retrieve OpenAPI specification for endpoint '{name}'. Details: {ex.Message}");
        }

        // Ensure the OpenAPI document has at least one server URL defined
        if (spec.Servers == null || spec.Servers.Count == 0 || string.IsNullOrEmpty(spec.Servers[0].Url))
        {
            _logger?.LogError("OpenAPI specification for endpoint '{EndpointName}' does not define a server URL.", name);
            return Results.Problem($"OpenAPI specification for endpoint '{name}' does not define a server URL.");
        }

        // Combine the configured BaseUrl with the server URL from the OpenAPI document
        // This forms the complete base URL for API calls described in the spec (e.g., https://petstore3.swagger.io/api/v3)
        if (!Uri.TryCreate(rootApiBaseUri, spec.Servers[0].Url, out var fullApiBaseUri))
        {
            _logger?.LogError("Failed to combine root API base URL '{RootApiBaseUri}' with OpenAPI server URL '{OpenApiServerUrl}' for endpoint '{EndpointName}'.", rootApiBaseUri, spec.Servers[0].Url, name);
            return Results.Problem($"Failed to construct full API base URL for endpoint '{name}'.");
        }

        // Calculate the relative path correctly by skipping the endpoint name segment from the incoming request
        var relativePath = string.Join('/', pathSegments.Skip(1));
        var fullApiBaseUrlString = new Uri(rootApiBaseUri, spec.Servers[0].Url).AbsoluteUri;
        // Construct the final target URI by appending the relative path to the full API base URI
        var targetUri = new Uri($"{fullApiBaseUrlString.TrimEnd('/')}/{relativePath.TrimStart('/')}");

        var proxyRequest = new HttpRequestMessage(new HttpMethod(request.Method), targetUri);

        // Inject authentication header if configured
        //if (!string.IsNullOrEmpty(specification.AuthenticationScheme) && !string.IsNullOrEmpty(specification.AuthValue))
        //{
        //    proxyRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
        //        specification.AuthenticationScheme, specification.AuthValue);
        //}

        // Copy request headers to the proxy request, excluding those managed by HttpClient or StreamContent
        foreach (var header in request.Headers)
        {
            // Skip headers that HttpClient automatically manages or are part of the content
            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                // If it's not a request header, try adding to content headers if content exists
                if (proxyRequest.Content != null)
                {
                    proxyRequest.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }
        }

        // Attach request body if present
        if (request.ContentLength > 0)
        {
            proxyRequest.Content = new StreamContent(request.Body);
            // Explicitly set Content-Type for the request body if present
            if (request.ContentType != null)
            {
                proxyRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.ContentType);
            }
        }

        using var client = _httpClientFactory.CreateClient(_options.HttpClientName);
        // Use HttpCompletionOption.ResponseHeadersRead for efficient streaming
        var response = await client.SendAsync(proxyRequest, HttpCompletionOption.ResponseHeadersRead, request.HttpContext.RequestAborted);
        // Copy response headers back to the original response
        foreach (var header in response.Headers)
        {
            request.HttpContext.Response.Headers[header.Key] = header.Value.ToArray();
        }
        // Also copy content headers from the proxy response
        foreach (var header in response.Content.Headers)
        {
            request.HttpContext.Response.Headers[header.Key] = header.Value.ToArray();
        }

        // Set the status code of the original response to match the proxied response
        request.HttpContext.Response.StatusCode = (int)response.StatusCode;

        switch (response.StatusCode)
        {
            case System.Net.HttpStatusCode.NotFound:
                return Results.NotFound(response);
            case System.Net.HttpStatusCode.Forbidden:
                return Results.Forbid();
            case System.Net.HttpStatusCode.Unauthorized:
                return Results.Unauthorized(); // Return Unauthorized for 401 status code
            default:
                return Results.Ok(response); // For any other status code, return OK with the response
        }
        //var responseBody = await response.Content.ReadAsStreamAsync();
        //return Results.Stream(responseBody, response.Content.Headers.ContentType?.ToString());
    }
}
