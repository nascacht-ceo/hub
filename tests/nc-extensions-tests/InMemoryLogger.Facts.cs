using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace nc.Extensions.Tests;

public class InMemoryLoggerFacts
{
	public class AddInMemoryLogger()
	{
		[Fact]
		public void CreatesSingleInstance()
		{
			var services = new ServiceCollection()
				.AddLogging(lb=>lb.AddInMemoryLogger())
				.BuildServiceProvider();
			var memoryLogger = services.GetRequiredService<InMemoryLogger>();

			var logger = services.GetRequiredService<ILogger<InMemoryLoggerFacts>>();
			using var scope = logger.BeginScope(memoryLogger.CreateScope("CreatesSingleInstance"));
			logger.LogInformation("Test log message 1");
			logger = services.GetRequiredService<ILogger<InMemoryLoggerFacts>>();
			logger.LogInformation("Test log message 2 ");
			Assert.Equal(2, memoryLogger.Messages.Count());
		}

		[Fact]
		public void IncludesNestedScopes()
		{
			var services = new ServiceCollection()
				.AddLogging(lb => lb.AddInMemoryLogger())
				.BuildServiceProvider();
			var memoryLogger = services.GetRequiredService<InMemoryLogger>();

			var logger = services.GetRequiredService<ILogger<InMemoryLoggerFacts>>();
			using var scope1 = logger.BeginScope(memoryLogger.CreateScope("NestLevel1"));
			using var scope2 = logger.BeginScope(memoryLogger.CreateScope("NestLevel2"));
			logger.LogInformation("Test log message 1");
			Assert.Equal(2, memoryLogger.Messages.Count());
		}

		[Fact]
		public async Task TracksNestedLoggers()
		{
			var services = new ServiceCollection()
				.AddLogging(lb => lb.AddInMemoryLogger())
				.BuildServiceProvider();
			var memoryLogger = services.GetRequiredService<InMemoryLogger>();

			var outerLogger = services.GetRequiredService<ILogger<InMemoryLoggerFacts>>();
			using var outerScope = outerLogger.BeginScope(memoryLogger.CreateScope("Outer Scope"));
			await Task.Run(() => 
			{ 
				var innerLogger = services.GetRequiredService<ILogger<InMemoryLogger>>();
				innerLogger.LogInformation("Inner Message");
			});
			Assert.Single(memoryLogger.Messages);
			Assert.Contains(memoryLogger.Messages, m => m.ScopeState!.Equals("Outer Scope"));
		}

		[Fact]
		public void IgnoresMessagesWithoutScope()
		{
			var services = new ServiceCollection()
				.AddLogging(lb => lb.AddInMemoryLogger())
				.BuildServiceProvider();
			var memoryLogger = services.GetRequiredService<InMemoryLogger>();

			var logger = services.GetRequiredService<ILogger<InMemoryLoggerFacts>>();
			logger.LogInformation("Test log message 1");
			logger.LogInformation("Test log message2 ");
			Assert.Empty(memoryLogger.Messages);
		}


		[Fact]
		public void UsesScopes()
		{
			var services = new ServiceCollection()
				.AddLogging(lb => lb.AddInMemoryLogger())
				.BuildServiceProvider();
			var memoryLogger = services.GetRequiredService<InMemoryLogger>();

			var logger = services.GetRequiredService<ILogger<InMemoryLoggerFacts>>();
			var guid = Guid.NewGuid().ToString();
			using var scope = logger.BeginScope(memoryLogger.CreateScope(guid));

			logger.LogInformation("Test log message 1");
			Assert.NotEmpty(memoryLogger.Messages);
			Assert.Contains(memoryLogger.Messages, m => m.ScopeState!.Equals(guid));

		}

		[Fact]
		public async Task WorksInParallel()
		{
			var services = new ServiceCollection()
				.AddLogging(lb => lb.AddInMemoryLogger())
				.BuildServiceProvider();
			var scopeProvider = services.GetService<IExternalScopeProvider>();
			var memoryLogger = services.GetRequiredService<InMemoryLogger>();

			var tasks = new List<Task>();
			var logger = services.GetRequiredService<ILogger<InMemoryLoggerFacts>>();
			for (int i = 0; i < 5; i++)
			{
				tasks.Add(Task.Run(() =>
				{
					var guid = Guid.NewGuid().ToString();
					using var scope = logger.BeginScope(memoryLogger.CreateScope($"WorksInParallel {guid}"));
					logger.LogInformation($"Test log message 1");
					logger.LogInformation($"Test log message 2");
					logger.LogInformation($"Test log message 3");
				}));

			}
			await Task.WhenAll(tasks);
			var groups = memoryLogger.Messages.GroupBy(m => m.ScopeState);
			Assert.Equal(5, groups.Count());


		}

		[Fact]
		public void DiscardsMessagesOnScopeDisposal()
		{
			var services = new ServiceCollection()
				.AddLogging(lb => lb.AddInMemoryLogger())
				.BuildServiceProvider();
			var memoryLogger = services.GetRequiredService<InMemoryLogger>();
			var logger = services.GetRequiredService<ILogger<InMemoryLoggerFacts>>();
			var guid = Guid.NewGuid().ToString();
			// todo: collapse this with a fluid extension method; memoryLogger.BeginTrace(logger, guid)?
			using (var trace = memoryLogger.CreateScope(guid))
			using (var scope = logger.BeginScope(trace))
			{
				logger.LogInformation("Test log message 1");
				Assert.NotEmpty(memoryLogger.Messages);
			}
			Assert.Empty(memoryLogger.Messages);
		}
	}

	public class GetMessages: InMemoryLoggerFacts
	{
		[Fact]
		public void ReturnsLoggedMessages()
		{
			var services = new ServiceCollection()
				.AddLogging(lb => lb.AddInMemoryLogger())
				.BuildServiceProvider();
			var memoryLogger = services.GetRequiredService<InMemoryLogger>();
			var logger = services.GetRequiredService<ILogger<InMemoryLoggerFacts>>();
			using var scope = logger.BeginScope(memoryLogger.CreateScope("GetLoggedMessages"));
			logger.LogInformation("Test log message 1");
			logger.LogInformation("Test log message 2 ");
			var messages = memoryLogger.GetScopedMessages("GetLoggedMessages");
			Assert.Equal(2, messages.Count());
		}

		[Fact]
		public void FiltersByScope()
		{
			var services = new ServiceCollection()
				.AddLogging(lb => lb.AddInMemoryLogger())
				.BuildServiceProvider();
			var memoryLogger = services.GetRequiredService<InMemoryLogger>();
			var logger = services.GetRequiredService<ILogger<InMemoryLoggerFacts>>();
			using (var scope = logger.BeginScope(memoryLogger.CreateScope("Scope1")))
			{
				logger.LogInformation("Test log message");
			}
			using (var scope = logger.BeginScope(memoryLogger.CreateScope("Scope2")))
			{
				logger.LogInformation("Test log message");
			}
			Assert.Single(memoryLogger.GetScopedMessages("Scope1"));
			Assert.Single(memoryLogger.GetScopedMessages("Scope2"));
		}
	}
}
