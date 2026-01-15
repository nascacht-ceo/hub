using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace nc.OpenApi;

public class OpenApiConfigureOptions : IConfigureOptions<OpenApiOptions>
{
	private readonly ILogger<OpenApiConfigureOptions>? _logger;

	public OpenApiConfigureOptions()
	{

	}
	public OpenApiConfigureOptions(ILogger<OpenApiConfigureOptions>? logger = null)
	{
		_logger = logger;
	}

	public void Configure(OpenApiOptions options)
	{
		_logger?.LogTrace("Configuring OpenApiService-driven endpoints.");
		options.AddDocumentTransformer((document, context, cancellationToken) =>
		{
			document.Paths.TryAdd("transformer", new Microsoft.OpenApi.OpenApiPathItem());
			//var service = context.ApplicationServices.GetService<OpenApiService>();
			//foreach (var endpoint in service!.ListEndpoints())
			//{
			//	var externalDocument = await service.GetSpecificationAsync(endpoint, cancellationToken);
			//	if (externalDocument == null)
			//		continue;

			//	// 1. Merge Paths
			//	foreach (var path in externalDocument.Paths)
			//	{
			//		// Use TryAdd to ensure your application's generated paths take precedence
			//		document.Paths.TryAdd(path.Key, path.Value);
			//	}


			//	// 2. Merge Components (Schemas, Security Schemes, etc.)
			//	if (externalDocument.Components != null)
			//	{
			//		// Merge Schemas
			//		foreach (var schema in externalDocument.Components.Schemas)
			//		{
			//			document.Components.Schemas.TryAdd(schema.Key, schema.Value);
			//		}
			//		// Merge Tags
			//		if (externalDocument.Tags != null)
			//		{
			//			document.Tags = document.Tags
			//				.Concat(externalDocument.Tags)
			//				.DistinctBy(t => t.Name)
			//				.ToList();
			//		}
			//		// Add security schemes, parameters, etc., similarly...
			//	}
			//}
			return Task.CompletedTask;
		});
	}
}
