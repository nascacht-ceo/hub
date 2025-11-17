using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nc.Extensions.Logging;

namespace nc.Extenstions.Logging.Tests;

public class QueueLoggerProviderFacts
{
	public class AddQueueLogger
	{
		private readonly ServiceProvider _services;

		public AddQueueLogger()
		{
			_services = new ServiceCollection()
				.AddLogging(builder => builder.AddQueueLogger())
				.BuildServiceProvider();
		}

		[Theory]
		[InlineData(typeof(IExternalScopeProvider))]
		public void AddsRequiredServices(Type serviceType)
		{
			Assert.NotNull(_services.GetService(serviceType));
		}

		[Theory]
		[InlineData(typeof(QueueLoggerProvider))]
		public void AddsLoggerProvider(Type loggerProviderType)
		{
			var loggerProviders = _services.GetServices<ILoggerProvider>();
			Assert.Contains(loggerProviders, p => p.GetType() == loggerProviderType);
		}
	}
}
