using Microsoft.Extensions.DependencyInjection;

namespace nc.Api;

public static class ApiServiceExtensions
{
	public static IServiceCollection AddNcServices(this IServiceCollection services)
	{
		return services;
	}
}
