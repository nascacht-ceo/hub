using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace nc.Api;

public static class StoreEndpoints
{
	public const string OpenApiTag = "stores";

	public static IEndpointRouteBuilder MapStoreEndpoints(this IEndpointRouteBuilder endpoints)
	{
		var storeGroup = endpoints
			.MapGroup("/api/nc/stores")
			.WithTags(OpenApiTag);
		// Add store-related endpoints here

		storeGroup.MapGet("", 
			async (CancellationToken cancellationToken) => {
				return Task.CompletedTask;
			}
		);
		return storeGroup;
	}
}
