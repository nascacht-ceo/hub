using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.ServiceModel;

namespace nc.Soap;

public class SoapService
{
	private readonly SoapServiceOptions _options;
	private readonly IDistributedCache _cache;
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly IStringLocalizer<SoapService> _localizer;
	private readonly ILogger<SoapService>? _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="SoapService"/> class, providing configuration options, an HTTP client
	/// factory, localization support, and optional distributed caching and logging capabilities.
	/// </summary>
	/// <param name="options">The monitor for <see cref="SoapServiceOptions"/> that provides configuration settings for the service. This
	/// parameter cannot be null.</param>
	/// <param name="httpClientFactory">The factory used to create <see cref="HttpClient"/> instances for making HTTP requests. This parameter cannot be
	/// null.</param>
	/// <param name="localizer">The <see cref="IStringLocalizer{T}"/> used for localizing strings in the service. This parameter cannot be null.</param>
	/// <param name="cache">An optional <see cref="IDistributedCache"/> instance for distributed caching. If not provided, caching will not be
	/// available.</param>
	/// <param name="logger">An optional <see cref="ILogger{T}"/> instance for logging diagnostic and error information.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> or <paramref name="httpClientFactory"/> is null.</exception>
	public SoapService(
		IOptionsMonitor<SoapServiceOptions> options,
		IHttpClientFactory httpClientFactory,
		IStringLocalizer<SoapService> localizer,
		IDistributedCache? cache = null,
		ILogger<SoapService>? logger = null
		)
	{
		_options = SetOptions(options);
		_cache = cache ?? throw new ArgumentNullException(nameof(cache), "Distributed cache cannot be null");
		_httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory), "HttpClient cannot be null");
		_localizer = localizer;
		_logger = logger;
	}

	/// <summary>
	/// Validates and retrieves the current <see cref="SoapServiceOptions"/> from the provided options monitor.
	/// </summary>
	/// <remarks>This method performs validation on the current <see cref="SoapServiceOptions"/> instance retrieved
	/// from the options monitor. If validation fails, the method logs the validation errors and returns the previously
	/// configured options instead of the invalid ones.</remarks>
	/// <param name="optionsMonitor">An <see cref="IOptionsMonitor{TOptions}"/> instance used to access the current <see cref="SoapServiceOptions"/>.</param>
	/// <returns>The validated <see cref="SoapServiceOptions"/> instance. If validation fails, the previously configured options are
	/// returned.</returns>
	private SoapServiceOptions SetOptions(IOptionsMonitor<SoapServiceOptions> optionsMonitor)
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
				nameof(SoapServiceOptions),
				typeof(SoapServiceOptions),
				validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error")
			);
			_logger?.LogError(exception, "SoapServiceOptions validation failed: {ValidationException}", string.Join(", ", validationResults.Select(r => r.ErrorMessage)));
			return _options; // Return the original options if validation fails
		}
		return options;
	}

	public async Task GetMetadataAsync(string name, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name), _localizer["OpenapiSpecNotRegistered"]);
		if (!_options.Specifications.ContainsKey(name))
			throw new ArgumentOutOfRangeException(nameof(name), name, _localizer["OpenapiSpecNotRegistered", string.Join(", ", _options.Specifications.Keys)]);

		var metadataUri = _options.Specifications[name].SpecUrl;

		// 1) Try HTTP GET (?wsdl)
		try
		{
			//var httpClient = new MetadataExchangeClient(metadataUri, MetadataExchangeClientMode.HttpGet)
			//{
			//	ResolveMetadataReferences = true,                    // follow <wsdl:import>/<xsd:import>
			//	OperationTimeout = TimeSpan.FromSeconds(30)
			//};

			//if (ep.BasicAuth is not null)
			//	httpClient.HttpCredentials = new NetworkCredential(ep.BasicAuth.UserName, ep.BasicAuth.Password);
			//else if (ep.UseDefaultCredentials)
			//	httpClient.HttpCredentials = CredentialCache.DefaultNetworkCredentials;

			//return await Task.Run(() => httpClient.GetMetadata(), ct);
		}
		catch
		{
			// fall through to WS-MEX attempt
		}

		// 2) Try WS-MEX (SOAP metadata)
		//var mexAddress = EndsWithMex(metadataUri) ? metadataUri : BuildMexAddress(ep.Address);
		//var mexBinding = mexAddress.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
		//	? System.ServiceModel.Description.MetadataExchangeBindings.CreateMexHttpsBinding()
		//	: MetadataExchangeBindings.CreateMexHttpBinding();

		//var mexClient = new MetadataExchangeClient(mexBinding)
		//{
		//	ResolveMetadataReferences = true,
		//	OperationTimeout = TimeSpan.FromSeconds(30)
		//};

		//// Map credentials for SOAP (WS-Transfer) metadata
		//if (ep.BasicAuth is not null)
		//{
		//	mexClient.SoapCredentials.UserName.UserName = ep.BasicAuth.UserName;
		//	mexClient.SoapCredentials.UserName.Password = ep.BasicAuth.Password;
		//}
		//else if (ep.UseDefaultCredentials)
		//{
		//	// Windows auth; usually nothing else required here
		//	// (mexClient.SoapCredentials.Windows settings can be tweaked if needed)
		//}

		//return await Task.Run(() => mexClient.GetMetadata(new EndpointAddress(mexAddress)), ct);
	}

	private static Uri DeriveMetadataUri(Uri address)
	{
		var u = address.ToString();
		if (EndsWithMex(address)) return address;
		if (u.Contains("?wsdl", StringComparison.OrdinalIgnoreCase)) return address;

		// Common cases: *.svc, *.asmx, arbitrary HTTP endpoint
		if (address.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
			return new Uri(u.Contains("?") ? (u + "&wsdl") : (u + "?wsdl"));

		// Non-HTTP (e.g., net.tcp) typically uses /mex
		return BuildMexAddress(address);
	}

	private static bool EndsWithMex(Uri uri)
		=> uri.AbsolutePath.EndsWith("/mex", StringComparison.OrdinalIgnoreCase)
		   || uri.Segments.LastOrDefault()?.Equals("mex", StringComparison.OrdinalIgnoreCase) == true;

	private static Uri BuildMexAddress(Uri baseAddress)
	{
		var builder = new UriBuilder(baseAddress);
		if (!builder.Path.EndsWith("/")) builder.Path += "/";
		builder.Path += "mex";
		builder.Query = string.Empty;
		return builder.Uri;
	}
}
