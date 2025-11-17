using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace nc.License;

public class LicenseService : IHostedService
{
	private readonly LicenseOptions _options;

	public LicenseService(IOptions<LicenseOptions> options)
	{
		_options = options.Value;
		if (_options.PublicKey == null && _options.FilePath == null)
			throw new ArgumentNullException(nameof(options), "LicenseOptions.PublicKey or FilePath must be provided in LicenseOptions.");

		if (_options.PublicKey == null)
		{
			if (!File.Exists(_options.FilePath!))
				throw new FileNotFoundException("License public key file not found.", _options.FilePath);
			_options.PublicKey = File.ReadAllText(_options.FilePath!);
		}
	}
	public Task StartAsync(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public bool VerifyLicense(string licenseKey)
	{
		// Implement license verification logic here
		return true;
	}
}
