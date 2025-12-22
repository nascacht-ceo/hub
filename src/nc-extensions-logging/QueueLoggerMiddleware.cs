using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace nc.Extensions.Logging;

public class QueueLoggerMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<QueueLoggerMiddleware> _logger;
	private readonly QueueScopeManager _traceManager;

	public QueueLoggerMiddleware(RequestDelegate next, QueueScopeManager traceManager, ILogger<QueueLoggerMiddleware> logger)
	{
		_next = next;
		_logger = logger;
		_traceManager = traceManager;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		if (!context.Request.Headers.TryGetValue("X-Trace-Id", out var traceId) || string.IsNullOrEmpty(traceId)) 
		{ 
			await _next(context); return; 
		}
		if (!_traceManager.TryGetScope(traceId!, out var queueScope)) 
		{ 
			_logger.LogWarning("Request has traceId {TraceId} but no active listener.", traceId!); 
			await _next(context); 
			return; 
		}
		using (_logger.BeginScope(queueScope!))
		{
			_logger.LogInformation("Middleware captured request for traceId {TraceId}", traceId!);
			try 
			{ 
				await _next(context); 
			}
			catch (Exception ex) 
			{ 
				_logger.LogCritical(ex, "Unhandled exception in traced request"); 
				throw; 
			}
		}
	}
}