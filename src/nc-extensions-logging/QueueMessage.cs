using Microsoft.Extensions.Logging;

namespace nc.Extensions.Logging;

public class QueueMessage
{
	public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;
	public LogLevel LogLevel { get; set; }
	public EventId EventId { get; set; }
	public required string Message { get; set; }
	public Exception? Exception { get; set; }
	public object? State { get; set; } 

	public object? Scope { get; set; }

	public override string ToString()
	{
		string scopePrefix = State != null ? $"[{State}] " : "";
		return $"{scopePrefix}[{Timestamp:HH:mm:ss.fff}] [{LogLevel}] {Message} {(Exception != null ? $"(Exception: {Exception.Message})" : "")}";
	}
}
