using Microsoft.Extensions.Logging;
using Nito.Disposables;

namespace nc.Extensions.Logging;

public class QueueLogger : ILogger
{
	private readonly string _categoryName;
	private readonly IExternalScopeProvider _scopeProvider;

	public QueueLogger(string categorName, IExternalScopeProvider scopeProvider)
	{
		_categoryName = categorName;
		_scopeProvider = scopeProvider;
	}

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull
	{
		return NoopDisposable.Instance;
	}

	public bool IsEnabled(LogLevel logLevel)
	{
		return true;
	}

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		if (!IsEnabled(logLevel))
			return;
		QueueMessage? message = null;
		_scopeProvider.ForEachScope((scope, _) =>
		{
			var queue = scope as QueueScope;
			if (queue == null)
				return;
			message ??= new QueueMessage
			{
				LogLevel = logLevel,
				EventId = eventId,
				State = state,
				Exception = exception,
				Message = formatter(state, exception),
				Scope = queue.State
			};
			queue.Queue.TryAdd(message);
		}, (object?)null);
	}
}
