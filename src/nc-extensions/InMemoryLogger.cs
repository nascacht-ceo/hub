using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Nito.Disposables;
using System.Collections.Concurrent;

namespace nc.Extensions;

public class LogMessage
{
	public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;
	public LogLevel Level { get; }
	public string Message { get; }
	public Exception? Exception { get; }
	public object? ScopeState { get; } // NEW: Stores the current scope state

	public LogMessage(LogLevel level, string message, Exception? exception, object? scopeState)
	{
		Level = level;
		Message = message;
		Exception = exception;
		ScopeState = scopeState;
	}

	public override string ToString()
	{
		// NEW: Include the scope state in the output for identification
		string scopePrefix = ScopeState != null ? $"[{ScopeState}] " : "";
		return $"{scopePrefix}[{Timestamp:HH:mm:ss.fff}] [{Level}] {Message} {(Exception != null ? $"(Exception: {Exception.Message})" : "")}";
	}
}

public interface ITraceLogger: ILogger
{
	public IDisposable BeginTrace(ILogger logger, object state);
}

/// <summary>
/// A thread-safe logger implementation for unit testing that captures messages in memory.
/// NOTE: Implements the standard Microsoft.Extensions.Logging.ILogger interface.
/// </summary>
public class InMemoryLogger : ILogger
{
	// Uses AsyncLocal to safely flow the scope state across async/await boundaries
	// private static readonly AsyncLocal<object?> _currentScope = new AsyncLocal<object?>();
	private readonly ConcurrentDictionary<object, ConcurrentBag<LogMessage>> _messages;
	private readonly IExternalScopeProvider _externalScopeProvider;

	public InMemoryLogger(IExternalScopeProvider externalScopeProvider)
	{
		_externalScopeProvider = externalScopeProvider;
		_messages = new ConcurrentDictionary<object, ConcurrentBag<LogMessage>>();
	}

	public IEnumerable<LogMessage> Messages => _messages.SelectMany(m => m.Value.ToList());

	public IEnumerable<LogMessage>? GetScopedMessages(object scope)
	{
		if (_messages.ContainsKey(scope))
			return _messages[scope];
		return null;
	}

	public IDisposable CreateScope(object state)
	{
		return new Trace(state, () => 
		{ 
			_messages.TryRemove(state!, out _);
		});
	}

	// Required by ILogger, but not used for filtering in this simple implementation
	public IDisposable BeginScope<TState>(TState state) where TState : notnull
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
		{
			return;
		}

		string message = formatter(state, exception);
		_externalScopeProvider.ForEachScope<string>((scope, m) =>
		{
			var trace = scope as Trace;
			if (trace == null)
				return;
			var scopedMessages = _messages.GetOrAdd(trace.State, _ => new ConcurrentBag<LogMessage>());
			scopedMessages.Add(new LogMessage(logLevel, m, exception, trace.State));

		}, message); 
	}


	private class Trace : IDisposable
	{
		private readonly Action _action;
		public readonly object State;
		public Trace(object state, Action action)
		{
			State = state;
			_action = action;
		}
		public void Dispose()
		{
			_action();
		}

		public override string ToString()
		{
			return State?.ToString() ?? string.Empty;
		}
	}
}

/// <summary>
/// ILoggerProvider implementation for InMemoryLogger.
/// This is the standard way to register custom loggers with the .NET logging pipeline.
/// </summary>
public class InMemoryLoggerProvider : ILoggerProvider
{
	private readonly IExternalScopeProvider _scopeProvider;

	// We use a single, shared instance of the concrete InMemoryLogger 
	// to collect all messages from all injected ILogger<T> instances.
	private readonly InMemoryLogger _sharedLoggerInstance;

	// Concurrent dictionary to hold a mapping of category names to logger instances
	private readonly ConcurrentDictionary<string, ILogger> _loggers =
		new ConcurrentDictionary<string, ILogger>();

	public InMemoryLoggerProvider(IExternalScopeProvider scopeProvider, InMemoryLogger? sharedLoggerInstance = null)
	{
		_scopeProvider = scopeProvider;
		_sharedLoggerInstance = sharedLoggerInstance ?? new InMemoryLogger(scopeProvider);
	}

	/// <summary>
	/// Creates a new ILogger instance for a given category.
	/// In this setup, we return the same underlying logger instance 
	/// but wrap it in a standard ILogger interface if needed.
	/// </summary>
	/// <param name="categoryName">The logger category name (e.g., the class name).</param>
	public ILogger CreateLogger(string categoryName)
	{
		// For simplicity and to ensure all logs go to the same central list, 
		// we return the shared logger instance. 
		return _sharedLoggerInstance;
	}

	public void Dispose()
	{
		// No resources to dispose in this simple provider.
	}
}

// Extension method to make registration clean
public static class InMemoryLoggerExtensions
{
	public static ILoggingBuilder AddInMemoryLogger(this ILoggingBuilder builder)
	{
		// var logger = new InMemoryLogger();
		builder.Services.TryAddSingleton<IExternalScopeProvider, LoggerExternalScopeProvider>();
		builder.Services.TryAddSingleton<InMemoryLogger>();
		builder.Services.AddSingleton<ILoggerProvider, InMemoryLoggerProvider>(sp =>
		{
			var scopeProvider = sp.GetRequiredService<IExternalScopeProvider>();
			var memoryLogger = sp.GetRequiredService<InMemoryLogger>();
			return new InMemoryLoggerProvider(scopeProvider, memoryLogger);
		});
		return builder;
	}
}
