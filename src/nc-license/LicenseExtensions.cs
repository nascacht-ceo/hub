using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace nc.License;

public static class LicenseExtensions
{
	/// <summary>
	/// Adds NastCacht license configuration to the specified service collection.
	/// </summary>
	/// <remarks>This method registers the provided license key for NastCacht in the application's dependency
	/// injection container. Call this method during application startup before building the service provider.</remarks>
	/// <param name="services">The service collection to which the NastCacht license configuration will be added.</param>
	/// <param name="licenseKey">The license key used to configure NastCacht licensing. Cannot be null.</param>
	/// <returns>The <see cref="IServiceCollection"/> instance with NastCacht license configuration added.</returns>
	public static IServiceCollection AddNastCachtLicense(this IServiceCollection services, string licenseKey)
	{
		services.Configure<LicenseOptions>(options =>
		{
			options.LicenseKey = licenseKey;
		});
		return AddNastCachtLicense(services);
	}

	/// <summary>
	/// Adds NastCacht license configuration services to the specified service collection using the provided configuration.
	/// </summary>
	/// <remarks>This method binds license-related configuration from the specified <paramref name="configuration"/>
	/// to the service options. It is intended to be called during application startup as part of the dependency injection
	/// setup.</remarks>
	/// <param name="services">The service collection to which the NastCacht license services will be added.</param>
	/// <param name="configuration">The configuration instance containing license settings to be bound to the service options.</param>
	/// <returns>The same instance of <see cref="IServiceCollection"/> that was provided, to support method chaining.</returns>
	public static IServiceCollection AddNastCachtLicense(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<LicenseOptions>(configuration);
		return AddNastCachtLicense(services);
	}

	/// <summary>
	/// Adds the LicenseService and its dependencies to the specified IServiceCollection for license verification and
	/// background processing.
	/// </summary>
	/// <remarks>This method configures an HTTP client for license verification with a predefined base address and
	/// timeout, registers LicenseService as a singleton, and adds it as a hosted service. Call this method during
	/// application startup to enable license validation features.</remarks>
	/// <param name="services">The IServiceCollection to which the LicenseService and related HTTP client are added.</param>
	/// <returns>The IServiceCollection instance with the LicenseService and its dependencies registered.</returns>
	private static IServiceCollection AddNastCachtLicense(this IServiceCollection services)
	{
		services
			.AddHttpClient(nameof(LicenseService), client =>
			{
				client.BaseAddress = new Uri(LicenseOptions.ApiVerificationAddress);
				client.DefaultRequestHeaders.Add("Accept", "application/json");
				client.Timeout = TimeSpan.FromSeconds(5);
			})
			.AddStandardResilienceHandler();
		services
			.AddSingleton<LicenseService>()
			.AddHostedService(sp => sp.GetRequiredService<LicenseService>());
		return services;
	}
}
