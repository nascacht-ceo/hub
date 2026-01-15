using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace nc.License;

/// <summary>
/// Provides license validation and management services for the application, including verification of license keys and
/// API-based license validation. This class is intended to be used as a hosted service within the application's
/// lifetime.
/// </summary>
/// <remarks>LicenseService supports both local license key verification and remote API-based validation,
/// depending on the configuration provided in LicenseOptions. It implements IHostedService, allowing license validation
/// to be performed during application startup. The service exposes the current license information and its validity
/// status for use by other components. This class is thread-safe for typical usage as a hosted service.</remarks>
public sealed class LicenseService : IHostedService
{
	private readonly LicenseOptions _options;
	private readonly IHttpClientFactory _httpClientFactory;

	/// <summary>
	/// Initializes a new instance of the LicenseService class using the specified license options and HTTP client factory.
	/// </summary>
	/// <param name="options">The options containing license configuration values, including the license key, license key file path, or API key.
	/// At least one of these must be provided.</param>
	/// <param name="httpClientFactory">The factory used to create HTTP client instances for network operations.</param>
	/// <exception cref="ArgumentNullException">Thrown if none of ApiKey, LicenseKey, or LicenseKeyPath are provided in the options parameter.</exception>
	/// <exception cref="FileNotFoundException">Thrown if LicenseKeyPath is specified in the options but the file does not exist.</exception>
	public LicenseService(IOptions<LicenseOptions> options, IHttpClientFactory httpClientFactory)
	{
		_options = options.Value;
		_httpClientFactory = httpClientFactory;
		if (_options.LicenseKey == null && _options.LicenseKeyPath == null && _options.ApiKey == null)
			throw new ArgumentNullException(nameof(options), "ApiKey, PublicKey, or FilePath must be provided in LicenseOptions.");

		if (_options.LicenseKey == null && _options.ApiKey == null)
		{
			if (!File.Exists(_options.LicenseKeyPath!))
				throw new FileNotFoundException("License public key file not found.", _options.LicenseKeyPath);
			_options.LicenseKey = File.ReadAllText(_options.LicenseKeyPath!);
		}
	}

	/// <summary>
	/// True if the associated license is valid.
	/// </summary>
	public bool IsValid { get; private set; }

	/// <summary>
	/// Gets the license information associated with the current instance.
	/// </summary>
	public License? License { get; private set; }

	/// <summary>
	/// Initializes the validation process by verifying the license or API key asynchronously.
	/// </summary>
	/// <remarks>If a license key is provided in the options, validation is performed synchronously; otherwise, API
	/// key validation is performed asynchronously. The result of the validation is reflected in the IsValid
	/// property.</remarks>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
	/// <returns>A task that represents the asynchronous start operation.</returns>
	public async Task StartAsync(CancellationToken cancellationToken = default)
	{
		if (_options.LicenseKey != null)
			IsValid = VerifyLicenseKey();
		else
			IsValid = await VerifyApiKeyAsync(cancellationToken);
	}

	/// <summary>
	/// Asynchronously stops the operation or service, performing any necessary cleanup.
	/// </summary>
	/// <param name="cancellationToken">A token that can be used to request cancellation of the stop operation.</param>
	/// <returns>A task that represents the asynchronous stop operation.</returns>
	/// <exception cref="NotImplementedException">The method is not implemented.</exception>
	public Task StopAsync(CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Asynchronously verifies whether the configured API key is valid and active by contacting the license validation
	/// service.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the verification operation.</param>
	/// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the API key is
	/// valid and active; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if the configured API key is <see langword="null"/>.</exception>
	private async Task<bool> VerifyApiKeyAsync(CancellationToken cancellationToken = default)
	{
		if (_options.ApiKey == null) throw new ArgumentNullException(nameof(_options.ApiKey));
		try
		{
			var client = _httpClientFactory.CreateClient(nameof(LicenseService));
			var content = new FormUrlEncodedContent([new KeyValuePair<string, string>("license_key", _options.ApiKey)]);
			var response = await client.PostAsync("https://api.lemonsqueezy.com/v1/licenses/validate", content, cancellationToken);

			if (!response.IsSuccessStatusCode) return false;

			var result = await response.Content.ReadFromJsonAsync<ApiValidationResponse>(cancellationToken);
			return result is { Valid: true } && result.LicenseKey?.Status == "active";
		}
		catch { return false; }
	}

	/// <summary>
	/// Verifies the authenticity and validity of the current license key using the configured public key.
	/// </summary>
	/// <remarks>This method checks both the digital signature of the license key and its expiration date. If the
	/// license key is invalid, expired, or cannot be verified, the method returns false.</remarks>
	/// <returns>true if the license key is authentic and has not expired; otherwise, false.</returns>
	private bool VerifyLicenseKey()
	{
		try
		{
			var parts = _options.LicenseKey!.Split('.');
			if (parts.Length != 2) return false;

			byte[] dataBytes = Convert.FromBase64String(parts[0]);
			byte[] signatureBytes = Convert.FromBase64String(parts[1]);

			using var rsa = RSA.Create();
			rsa.ImportFromPem(LicenseOptions.NascachtPublicKey);

			// Verify the math
			bool isAuthentic = rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

			if (isAuthentic)
			{
				// Hydrate the record using the JSON property names (sub, org, exp)
				string json = Encoding.UTF8.GetString(dataBytes);
				License = JsonSerializer.Deserialize<License>(json);
				if (License?.ExpirationDate == null) return false;
				return License.ExpirationDate! > DateOnly.FromDateTime(DateTime.Now);
			}

			return isAuthentic;
		}
		catch { return false; }
	}
}
