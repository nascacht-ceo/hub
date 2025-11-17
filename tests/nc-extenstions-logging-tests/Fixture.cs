using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using nc.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace nc.Extenstions.Logging.Tests;

/// <summary>
/// This is a dummy entry point class for WebApplicationFactory
/// </summary>
public class TestStartup { }

/// <summary>
/// This is the xUnit test fixture. It creates a single instance
/// of the in-memory web application (IHost) that all tests
/// in a class can share.
/// </summary>
[CollectionDefinition(nameof(Fixture))]
public class Fixture : WebApplicationFactory<TestStartup>, IAsyncLifetime
{
	public Fixture() { }

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		// This is where we manually build the host,
		// replicating what Program.cs does.

		builder.ConfigureLogging(logging =>
		{
			logging.ClearProviders();
			logging.AddConsole();
			logging.AddQueueLogger();
			logging.SetMinimumLevel(LogLevel.Trace);
		});

		builder.ConfigureServices(services =>
		{
			services.AddSingleton<QueueScopeManager>();
			services.AddSignalR();
		});

		builder.Configure(app =>
		{
			app.UseWebSockets();
			app.UseMiddleware<QueueLoggerMiddleware>();

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapGet("/", async context =>
				{
					var logger = context.RequestServices.GetRequiredService<ILogger<TestStartup>>();
					logger.LogInformation("Handling GET request for /");
					using (logger.BeginScope("BusinessLogic-{Step}", 1))
					{
						logger.LogWarning("This is a warning inside a scope.");
					}
					logger.LogInformation("Finished handling request.");
					await context.Response.WriteAsJsonAsync(new { Message = "Request completed successfully." });
				});

				endpoints.MapGet("/error", async context =>
				{
					var logger = context.RequestServices.GetRequiredService<ILogger<TestStartup>>();
					logger.LogInformation("Handling GET request for /error");
					try { throw new InvalidOperationException("This is a simulated exception."); }
					catch (Exception ex) { logger.LogError(ex, "Caught an expected exception."); }
					await context.Response.WriteAsJsonAsync(Results.Problem("An error occurred."));
				});

				endpoints.MapHub<QueueLoggerHub>("/logHub");
			});
		});
	}

	public Task InitializeAsync() => Task.CompletedTask;

	async Task IAsyncLifetime.DisposeAsync()
	{
		await base.DisposeAsync();
	}
}
