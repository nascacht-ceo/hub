using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nc.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace nc.Extenstions.Logging.Tests;

public class QueueLoggerFacts
{
	public class Log : QueueLoggerFacts
	{
		[Fact]
		public void EnqueuesMessages()
		{
			var services = new ServiceCollection()
				.AddLogging(builder => builder.AddQueueLogger())
				.BuildServiceProvider();

			var logger = services.GetRequiredService<ILogger<QueueLoggerFacts>>();
			using var queue = new QueueScope();
			using var scope = logger.BeginScope(queue);
			logger.LogInformation("Test message");
			Assert.NotEmpty(queue.Queue);
		}

		[Fact]
		public void IncludesQueueState()
		{
			var services = new ServiceCollection()
				.AddLogging(builder => builder.AddQueueLogger())
				.BuildServiceProvider();

			var logger = services.GetRequiredService<ILogger<QueueLoggerFacts>>();
			using var queue = new QueueScope("IncludesQueueState");
			using var scope = logger.BeginScope(queue);
			logger.LogInformation("Test message");
			Assert.NotEmpty(queue.Queue);
			var message = queue.Queue.First();
			Assert.Equal("IncludesQueueState", message.Scope);
		}

		[Fact]
		public void RetainsState()
		{
			var services = new ServiceCollection()
				.AddLogging(builder => builder.AddQueueLogger())
				.BuildServiceProvider();

			var logger = services.GetRequiredService<ILogger<QueueLoggerFacts>>();
			using var queue = new QueueScope("IncludesQueueState");
			using var scope = logger.BeginScope(queue);
			logger.LogInformation("State test for {first} {last}", "Issac", "Newton");
			Assert.NotEmpty(queue.Queue);
			var message = queue.Queue.First();
			var values = message.State as IReadOnlyList<KeyValuePair<string, object?>>;
			Assert.NotNull(values);
			Assert.Equal(3, values!.Count);
		}

		[Fact]
		public void WorksWithExternalQueue()
		{
			var services = new ServiceCollection()
				.AddLogging(builder => builder.AddQueueLogger())
				.BuildServiceProvider();

			var queue = new ConcurrentQueue<QueueMessage>();
			var logger = services.GetRequiredService<ILogger<QueueLoggerFacts>>();
			logger.LogInformation("Does not appear in queue.");
			Assert.Empty(queue);

			using (var scope = logger.BeginScope(new QueueScope(queue: queue)))
			{
				logger.LogInformation("Appears in queue.");
				Assert.NotEmpty(queue);
			}
			Assert.NotEmpty(queue);
		}

		[Fact]
		public void DoesNotBlockWhenFull()
		{
			var services = new ServiceCollection()
				.AddLogging(builder => builder.AddQueueLogger())
				.BuildServiceProvider();
			var logger = services.GetRequiredService<ILogger<QueueLoggerFacts>>();
			using var queue = new QueueScope(maxSize: 2);
			using var scope = logger.BeginScope(queue);
			logger.LogInformation("Message 1");
			logger.LogInformation("Message 2");
			logger.LogInformation("Message 3");
			Assert.Equal(2, queue.Queue.Count);
		}

		[Fact]
		public void CompletesAddingOnDispose()
		{
			var services = new ServiceCollection()
				.AddLogging(builder => builder.AddQueueLogger())
				.BuildServiceProvider();
			var logger = services.GetRequiredService<ILogger<QueueLoggerFacts>>();
			var queue = new QueueScope();
			using (var scope = logger.BeginScope(queue))
			{
				logger.LogInformation("Message before dispose");
			}
			Assert.True(queue.Queue.IsAddingCompleted);
		}

		[Fact]
		public void WorksWithNestedLoggers()
		{
			var services = new ServiceCollection()
				.AddLogging(builder => builder.AddQueueLogger())
				.BuildServiceProvider();
			var queue = new QueueScope();
			var logger = services.GetRequiredService<ILogger<QueueLoggerFacts>>();
			using (var scope = logger.BeginScope(queue))
			{
				var nestedLogger = services.GetRequiredService<ILogger<NestedLogger>>();
				nestedLogger.LogInformation("Message from nested logger");
			}
			Assert.NotEmpty(queue.Queue);
		}

		private class NestedLogger
		{ }
	}
}
