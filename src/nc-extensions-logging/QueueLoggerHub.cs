using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace nc.Extensions.Logging;
/// <summary>
/// SignalR Hub for streaming logs from a QueueScope.
/// REFACTORED: The Hub now manages the QueueScope lifetime
/// using OnConnectedAsync and OnDisconnectedAsync.
/// </summary>
public class QueueLoggerHub : Hub
{
	private readonly QueueScopeManager _scopeManager;
	private readonly ILogger<QueueLoggerHub> _logger;

	public QueueLoggerHub(QueueScopeManager scopeManager, ILogger<QueueLoggerHub> logger)
	{
		_scopeManager = scopeManager;
		_logger = logger;
	}

	/// <summary>
	/// Called when a new client connects to the Hub.
	/// This is where we create and register the trace.
	/// </summary>
	public override async Task OnConnectedAsync()
	{
		string traceId = Context.ConnectionId;
		_logger.LogInformation("Client connected: {TraceId}. Creating trace...", traceId);

		// 1. Create the QueueScope.
		var queueScope = new QueueScope(traceId);

		// 2. Register it with the singleton manager.
		if (!_scopeManager.TryRegisterScope(traceId, queueScope))
		{
			// This should rarely happen, but if it does, log it.
			_logger.LogWarning("TraceId {TraceId} was already in use.", traceId);
			// We can dispose the new one and use the old one.
			queueScope.Dispose();
		}

		await base.OnConnectedAsync();
	}

	/// <summary>
	/// Called when a client disconnects.
	/// This is where we clean up the trace.
	/// </summary>
	public override Task OnDisconnectedAsync(Exception? exception)
	{
		string traceId = Context.ConnectionId;
		_logger.LogInformation("Client disconnected: {TraceId}. Cleaning up trace.", traceId);

		// 4. Unregister and Dispose.
		// This will call Dispose() on the QueueScope,
		// which calls CompleteAdding(), which gracefully
		// terminates the StreamLogs `await foreach` loop.
		_scopeManager.UnregisterScope(traceId);

		return base.OnDisconnectedAsync(exception);
	}

	/// <summary>
	/// A simple method clients can call to get their traceId.
	/// </summary>
	public string GetTraceId()
	{
		return Context.ConnectionId;
	}

	/// <summary>
	/// This is a server-to-client streaming method.
	/// The client just calls "StreamLogs" (no params)
	/// </summary>
	public async IAsyncEnumerable<QueueMessage> StreamLogs([EnumeratorCancellation] CancellationToken ct)
	{
		string traceId = Context.ConnectionId;
		_logger.LogInformation("Client {TraceId} starting log stream.", traceId);

		// 1. Get the trace (which *must* exist, created in OnConnectedAsync)
		if (!_scopeManager.TryGetScope(traceId, out var queueScope))
		{
			_logger.LogError("Could not find QueueScope for traceId {TraceId}. This should not happen.", traceId);
			yield break;
		}

		_logger.LogInformation("Streaming logs for {TraceId}...", traceId);
		try
		{
			// 2. Stream logs.
			// This loop will wait until a log appears, or
			// OnDisconnectedAsync is called, which disposes the scope
			// and completes the queue, ending the loop.
			if (queueScope == null)
			{
				_logger.LogError("QueueScope is null for {TraceId}.", traceId);
				yield break;
			}
			foreach (var log in queueScope.Queue.GetConsumingEnumerable(ct))
			{
				yield return log;
				await Task.Yield();
			}
		}
		finally
		{
			_logger.LogInformation("Stream finished for {TraceId}.", traceId);
		}
	}
}