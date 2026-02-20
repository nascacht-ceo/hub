using Microsoft.Extensions.DependencyInjection;
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

		
	}
}
