using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nc.Extensions.Logging;

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
public class Fixture : IAsyncLifetime
{
	public IHost TestHost { get; private set; }

	public Fixture() { }

	//protected override IWebHostBuilder? CreateWebHostBuilder()
	//{
	//	var builder = base.CreateWebHostBuilder();
	//	if (builder != null)
	//	{
	//		// Use the test project's current directory as content root.
	//		// If you need the web project's folder, change this to the relative path
	//		// e.g. Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "src", "nc-web")
	//		builder.UseContentRoot(Directory.GetCurrentDirectory());
	//	}
	//	return builder;
	//}

	protected void ConfigureWebHost(IWebHostBuilder builder)
	{

		// builder.UseContentRoot(Directory.GetCurrentDirectory());
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

	public async Task InitializeAsync()
	{
		TestHost = Host.CreateDefaultBuilder()
		   .ConfigureWebHostDefaults(webBuilder =>
		   {
			   webBuilder
				   .UseTestServer()
				   .ConfigureLogging(logging =>
				   {
					   logging.ClearProviders();
					   logging.AddConsole();
					   logging.AddDebug();
					   logging.AddQueueLogger();
					   logging.SetMinimumLevel(LogLevel.Trace);
				   })
				   .ConfigureServices(services =>
				   {
					   services.AddSingleton<QueueScopeManager>();
					   services.AddSignalR();
				   })
				   .Configure(app =>
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
		   })
		   .Start();

		//HubContext = Host.Services.GetRequiredService<IHubContext<QueueLoggerHub>>();

		//// Create a client connection to the hub
		//ClientConnection = new HubConnectionBuilder()
		//	.WithUrl("http://localhost/queue", o =>
		//	{
		//		o.HttpMessageHandlerFactory = _ => Host.GetTestClient().Handler;
		//	})
		//	.Build();

		//await ClientConnection.StartAsync();
	}

	async Task IAsyncLifetime.DisposeAsync()
	{
		await TestHost.StopAsync();
	}
}
