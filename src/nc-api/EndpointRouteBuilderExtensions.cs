using Microsoft.AspNetCore.Routing;

namespace nc.Api;

public static class EndpointRouteBuilderExtensions
{
	/// <summary>
	/// Maps the Nascacht-related endpoints to the specified <see cref="IEndpointRouteBuilder"/>.
	/// </summary>
	/// <remarks>This method configures the necessary routes for Nascacht functionality by mapping its associated
	/// endpoints. It is intended to be called during application startup as part of endpoint configuration.</remarks>
	/// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to which the endpoints will be mapped.</param>
	/// <returns>The <see cref="IEndpointRouteBuilder"/> instance with the Nascacht endpoints mapped.</returns>
	public static IEndpointRouteBuilder MapNascachtEndpoints(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapEncryptionStoreEndpoints();
		endpoints.MapOpenApiEndpoints();
		return endpoints;
	}
}
