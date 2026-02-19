using Microsoft.Extensions.DependencyInjection;
using nc.Ai.Caching;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
namespace nc.Ai.Tests;

public class AiServiceExtensionFacts
{
	public class AddAiServices: AiServiceExtensionFacts
	{
		private readonly ServiceProvider _services;

		public AddAiServices()
		{
			_services = new ServiceCollection().AddAiServices(new AiOptions()).BuildServiceProvider();
		}

		[Fact]
		public void InjectsCacheStrategy()
		{
			Assert.NotNull(_services.GetServices<ICacheStrategy>().OfType<PassthroughCacheStrategy>());
		}
	}
}
