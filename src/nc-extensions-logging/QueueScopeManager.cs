using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace nc.Extensions.Logging;

/// <summary>
/// This singleton service manages the mapping between trace IDs
/// and the QueueScopes that hold their logs.
/// </summary>
public class QueueScopeManager
{
	private readonly ConcurrentDictionary<string, QueueScope> _activeTraces = new();
	private readonly ILogger<QueueScopeManager> _logger;

	public QueueScopeManager(ILogger<QueueScopeManager> logger)
	{
		_logger = logger;
	}

	/// <summary>
	/// Called by the SignalR Hub when a client connects.
	/// </summary>
	public bool TryRegisterTrace(string traceId, QueueScope queueScope)
	{
		if (_activeTraces.TryAdd(traceId, queueScope))
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
	public void UnregisterTrace(string traceId)
	{
		if (_activeTraces.TryRemove(traceId, out var queueScope))
		{
			_logger.LogInformation("Trace {TraceId} unregistered.", traceId);
			// This will call CompleteAdding() on the queue
			queueScope.Dispose();
		}
	}

	/// <summary>
	/// Called by the Middleware when an HTTP request arrives.
	/// </summary>
	public bool TryGetTrace(string traceId, out QueueScope? queueScope)
	{
		return _activeTraces.TryGetValue(traceId, out queueScope);
	}
}