using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using nc.OpenApi;

namespace nc.Api;

public static class OpenApiEndpoints
{
	public const string OpenApiTag = "openapi";
	public static IEndpointRouteBuilder MapOpenApiEndpoints(this IEndpointRouteBuilder endpoints)
	{
		var documentation = endpoints.ServiceProvider.GetService<IStringLocalizer<OpenApi.Resources.Documentation>>();
		var openApiService = endpoints.ServiceProvider.GetService<OpenApiService>()
			?? throw new InvalidOperationException(documentation?[nameof(OpenApi.Resources.Errors.OpenApiServiceNotRegistered)].Value);
		var group = endpoints
			.MapGroup("/api/nc/openapi")
			.WithTags(OpenApiTag)
			.WithDescription(documentation?[nameof(OpenApi.Resources.Documentation.OpenApi)] ?? Resources.Errors.OpenApiDescriptionNotDefined);

		group.MapGet("", () =>
		{
			return Results.Ok(openApiService.ListEndpoints());
		}).WithDescription(documentation?[nameof(OpenApiService.ListEndpoints)].Value ?? "Lists all available OpenAPI endpoints.")
		  .WithName("GetOpenApiEndpoints")
		  .Produces<IEnumerable<string>>(StatusCodes.Status200OK);

		int count = 0;
		foreach (var endpoint in openApiService.ListEndpoints())
		{
			group.MapGet($"{endpoint}", async (HttpContext context) =>
			{
				var accept = context.Request.GetTypedHeaders().Accept;
				var wantsYaml = accept?.Any(h => h.MediaType.Value?.Contains("yaml", StringComparison.OrdinalIgnoreCase) ?? false) == true;

				var doc = await openApiService.GetSpecificationAsync(endpoint, context.RequestAborted);

				using var stream = new MemoryStream();
				if (wantsYaml)
				{
					context.Response.ContentType = "application/yaml";
					doc.SerializeAsYaml(stream, OpenApiSpecVersion.OpenApi3_0);
					await stream.CopyToAsync(context.Response.Body);
				}
				else
				{
					context.Response.ContentType = "application/json";
					doc.SerializeAsJson(stream, OpenApiSpecVersion.OpenApi3_0);
					await stream.CopyToAsync(context.Response.Body);
				}
				stream.Position = 0;
				return Results.File(stream.ToArray(), context.Response.ContentType!);
			}).WithDescription(documentation?[nameof(OpenApi.Resources.Documentation.EndpointDescription), endpoint].Value ?? $"OpenApi specification for ${endpoint}.")
			.WithName(endpoint)
			.Produces<string>(StatusCodes.Status200OK);
		}

		return endpoints;
	}
}
