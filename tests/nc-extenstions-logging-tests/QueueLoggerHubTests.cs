using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using nc.Extensions.Logging;

namespace nc.Extenstions.Logging.Tests;

internal class QueueLoggerHubTests
{
}

/// <summary>
/// This is the test class that uses the WebAppFixture.
/// The test code itself is unchanged.
/// </summary>
public class QueueLoggerHubTest : IClassFixture<Fixture>
{
	private readonly Fixture _fixture;

	public QueueLoggerHubTest(Fixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task HubAndMiddleware_EndToEnd_ShouldStreamLogs()
	{
		// --- ARRANGE ---

		// 1. Create a SignalR Hub Connection
		var connection = new HubConnectionBuilder()
			.WithUrl("http://localhost/logHub", o =>
			{
				// Use the fixture's in-memory server handler
				o.HttpMessageHandlerFactory = _ => _fixture.Server.CreateHandler();
			})
			.Build();

		// 2. Create an HttpClient to make web requests
		var httpClient = _fixture.CreateClient();

		// 3. Create a list to store received logs
		var receivedLogs = new List<QueueMessage>();
		var streamComplete = new TaskCompletionSource<bool>();

		await connection.StartAsync();

		// 4. Get the traceId from the Hub
		var traceId = await connection.InvokeAsync<string>("GetTraceId");
		Assert.NotNull(traceId);
		Assert.Equal(traceId, connection.ConnectionId);

		// 5. Start streaming logs. We use a ChannelReader.
		var channel = await connection.StreamAsChannelAsync<QueueMessage>("StreamLogs");

		// Start a background task to read from the stream
		_ = Task.Run(async () =>
		{
			try
			{
				await foreach (var log in channel.ReadAllAsync())
				{
					receivedLogs.Add(log);
				}
				// When the loop finishes, the stream is complete.
				streamComplete.TrySetResult(true);
			}
			catch (Exception ex)
			{
				streamComplete.TrySetException(ex);
			}
		});


		// --- ACT ---

		// 1. Create the HTTP request with the X-Trace-Id header
		var request = new HttpRequestMessage(HttpMethod.Get, "/");
		request.Headers.Add("X-Trace-Id", traceId);

		// 2. Send the HTTP request
		var response = await httpClient.SendAsync(request);
		response.EnsureSuccessStatusCode();

		// 3. Disconnect the SignalR client. This triggers OnDisconnectedAsync
		// on the server, which disposes the QueueScope, which calls
		// CompleteAdding() on the queue, which ends the `await foreach` loop.
		await connection.StopAsync();


		// --- ASSERT ---

		// 1. Wait for the streaming task to complete
		await streamComplete.Task;

		// 2. Check the logs we received.
		// Based on the "/" endpoint, we expect 4 logs:
		// 1. "Middleware captured request..." (from Middleware)
		// 2. "Handling GET request for /" (from Endpoint)
		// 3. "This is a warning inside a scope." (from Endpoint)
		// 4. "Finished handling request." (from Endpoint)

		// Note: The middleware logs *first*
		Assert.Equal(4, receivedLogs.Count);

		Assert.Contains(receivedLogs, log => log.Message.Contains("Middleware captured request for traceId"));
		Assert.Contains(receivedLogs, log => log.Message.Contains("Handling GET request for /"));
		Assert.Contains(receivedLogs, log => log.Message.Contains("This is a warning inside a scope."));
		Assert.Contains(receivedLogs, log => log.Message.Contains("Finished handling request."));

		// Check log levels
		Assert.Equal(3, receivedLogs.Count(l => l.LogLevel == LogLevel.Information));
		Assert.Equal(1, receivedLogs.Count(l => l.LogLevel == LogLevel.Warning));
	}
}
