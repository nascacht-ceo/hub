using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace nc.Extensions.Logging;

/// <summary>
/// This singleton service manages the mapping between trace IDs
/// and the QueueScopes that hold their logs.
/// </summary>
public class QueueScopeManager
{
	private readonly ConcurrentDictionary<string, QueueScope> _queueScopes = new();
	private readonly ILogger<QueueScopeManager> _logger;

	public QueueScopeManager(ILogger<QueueScopeManager> logger)
	{
		_logger = logger;
	}

	/// <summary>
	/// Called by the SignalR Hub when a client connects.
	/// </summary>
	public bool TryRegisterScope(string traceId, QueueScope queueScope)
	{
		if (_queueScopes.TryAdd(traceId, queueScope))
		{
			_logger.LogInformation("Trace {TraceId} registered.", traceId);
			return true;
		}
		_logger.LogWarning("Trace {TraceId} already exists.", traceId);
		return false;
	}

	/// <summary>
	/// Called by the SignalR Hub when a client disconnects.
	/// </summary>
	public void UnregisterScope(string traceId)
	{
		if (_queueScopes.TryRemove(traceId, out var queueScope))
		{
			_logger.LogInformation("Trace {TraceId} unregistered.", traceId);
			// This will call CompleteAdding() on the queue
			queueScope.Dispose();
		}
	}

	/// <summary>
	/// Called by the Middleware when an HTTP request arrives.
	/// </summary>
	public bool TryGetScope(string traceId, out QueueScope? queueScope)
	{
		return _queueScopes.TryGetValue(traceId, out queueScope);
	}
}