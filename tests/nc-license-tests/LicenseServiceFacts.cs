using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace nc.License.Tests;

public class LicenseServiceFacts
{
	public class VerifyPrivateKey: LicenseServiceFacts
	{
		[Fact]
		public async Task DecryptsKey()
		{
			var licenseKey = GenerateKey(new License("Google", "Commercial", DateOnly.MaxValue));
			var services = new ServiceCollection().AddNastCachtLicense(licenseKey).BuildServiceProvider();
			var licenseService = services.GetRequiredService<LicenseService>();
			await licenseService.StartAsync();
			Assert.True(licenseService.IsValid);
			Assert.Equal("Google", licenseService.License?.Subject);
		}

		[Fact]
		public async Task EnforcesExpiration()
		{
			var licenseKey = GenerateKey(new License("Google", "Commercial", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))));
			var services = new ServiceCollection().AddNastCachtLicense(licenseKey).BuildServiceProvider();
			var licenseService = services.GetRequiredService<LicenseService>();
			await licenseService.StartAsync();
			Assert.False(licenseService.IsValid);
		}

		[Fact]
		public async Task InvalidWithBadLicense()
		{
			var services = new ServiceCollection().AddNastCachtLicense("invalid-license").BuildServiceProvider();
			var licenseService = services.GetRequiredService<LicenseService>();
			await licenseService.StartAsync();
			Assert.False(licenseService.IsValid);
		}
	}

	public class VerifyApiKey: LicenseServiceFacts
	{ 		
		[Fact]
		public async Task InvalidWithBadApiKey()
		{
			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string?>
				{
					["nc:license:ApiKey"] = "this-is-a-invalid-api-key-for-testing"
				})
				.Build();
			var services = new ServiceCollection()
				.AddNastCachtLicense(configuration.GetSection("nc:license"))
				.BuildServiceProvider();
			var licenseService = services.GetRequiredService<LicenseService>();
			await licenseService.StartAsync();
			Assert.False(licenseService.IsValid);
		}
	}

	protected static string GenerateKey(License license)
	{
		var config = new ConfigurationBuilder().AddUserSecrets("nc-hub").Build();
		using var rsa = RSA.Create();
		rsa.ImportFromPem(config["nc:license:PrivateKey"]);

		string jsonPayload = JsonSerializer.Serialize(license);
		byte[] payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);
		string base64Payload = Convert.ToBase64String(payloadBytes);

		byte[] signatureBytes = rsa.SignData(
			payloadBytes,
			HashAlgorithmName.SHA256,
			RSASignaturePadding.Pkcs1);

		string base64Signature = Convert.ToBase64String(signatureBytes);

		return $"{base64Payload}.{base64Signature}";
	}
}
