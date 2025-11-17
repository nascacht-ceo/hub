using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace nc.Extensions.Logging;

public class QueueLoggerProvider : ILoggerProvider
{
	private readonly IExternalScopeProvider _scopeProvider;

	public QueueLoggerProvider(IExternalScopeProvider scopeProvider)
	{
		_scopeProvider = scopeProvider;
	}

	public ILogger CreateLogger(string categoryName)
	{
		return new QueueLogger(categoryName, _scopeProvider);
	}

	public void Dispose()
	{
		throw new NotImplementedException();
	}
}

public static class QueueLoggerProviderExtensions
{
	public static ILoggingBuilder AddQueueLogger(this ILoggingBuilder builder)
	{
		builder.Services.TryAddSingleton<IExternalScopeProvider, LoggerExternalScopeProvider>();
		builder.Services.AddSingleton<ILoggerProvider, QueueLoggerProvider>();
		return builder;
	}
}