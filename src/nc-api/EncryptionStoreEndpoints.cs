using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using nc.Cloud;
using nc.Cloud.Models.Encryption;

namespace nc.Api;

public static class EncryptionStoreEndpoints
{
	public const string OpenApiTag = "encryption";

	public static IEndpointRouteBuilder MapEncryptionStoreEndpoints(this IEndpointRouteBuilder endpoints)
	{
		var encryptionGroup = endpoints.MapGroup("/api/nc/encryption").WithTags(OpenApiTag);
		encryptionGroup.MapGet("/{name}/{id}",
			async (string name, string id, [FromServices]IServiceProvider keyedServices, CancellationToken cancellationToken) =>
			{
				var store = keyedServices.GetKeyedService<IEncryptionStore>(name);
				if (store == null)
				{
					return Results.NotFound($"Encryption store '{name}' not found.");
				}
				var keyPair = await store.GetKeyPairAsync(id);
				return keyPair is null ? Results.NotFound($"Key pair with ID '{id}' not found in store '{name}'.") : Results.Ok(keyPair);
			}
		);

		encryptionGroup.MapGet("/{id}",
			async (string id, [FromServices] IEncryptionStore store, CancellationToken cancellationToken) =>
			{
				var keyPair = await store.GetKeyPairAsync(id);
				return keyPair is null ? Results.NotFound($"Key pair with ID '{id}' not found.") : Results.Ok(keyPair);
			}
		);

		encryptionGroup.MapPost("/{name}/{id}",
			async (string name, string id, KeyPair keyPair, [FromServices]IServiceProvider keyedServices, CancellationToken cancellationToken) =>
			{
				var store = keyedServices.GetKeyedService<IEncryptionStore>(name);
				if (store == null)
				{
					return Results.NotFound($"Encryption store '{name}' not found.");
				}
				await store.SetKeyPairAsync(id, keyPair);
				return Results.Ok();
			}
		);

		encryptionGroup.MapPost("/{id}",
			async (string id, KeyPair keyPair, [FromServices] IEncryptionStore store, CancellationToken cancellationToken) =>
			{
				await store.SetKeyPairAsync(id, keyPair);
				return Results.Ok();
			}
		);

		encryptionGroup.MapPost("/{name}/{id}/create",
			async (string name, string id, [FromServices] IServiceProvider keyedServices, CancellationToken cancellationToken) =>
			{
				var store = keyedServices.GetKeyedService<IEncryptionStore>(name);
				if (store == null)
				{
					return Results.NotFound($"Encryption store '{name}' not found.");
				}
				var keyPair = KeyPair.Create(id);
				await store.SetKeyPairAsync(id, keyPair);
				return Results.Ok(keyPair);
			}
		);

		encryptionGroup.MapPost("/{id}/create",
			async (string id, [FromServices] IEncryptionStore store, CancellationToken cancellationToken) =>
			{
				var keyPair = KeyPair.Create(id);
				await store.SetKeyPairAsync(id, keyPair);
				return Results.Ok(keyPair);
			}
		);

		return endpoints;
	}
}
