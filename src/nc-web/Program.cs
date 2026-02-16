using nc.Api;
using nc.Extensions.DependencyInjection;
using Scalar.AspNetCore;


// 1. BUILDER SETUP
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");

// 2. Configruation
var section = builder.Configuration.GetSection("nc");

// 3. SERVICES REGISTRATION
builder.Services.AddNascachtAmazonServices(section);
builder.Services.AddNascachtOpenApiService(section);
// builder.Services.AddNascachtAzureServices(section); 
// builder.Services.AddNascachtGcpServices(section);
// builder.Services.AddNascachtAiServices(section);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
	//options.AddDocumentTransformer(async (document, context, cancellationToken) =>
	//{
	//	var service = context.ApplicationServices.GetService<nc.OpenApi.OpenApiService>();
	//	foreach (var endpoint in service!.ListEndpoints())
	//	{
	//		var externalDocument = await service.GetSpecificationAsync(endpoint, cancellationToken);
	//		if (externalDocument == null)
	//			continue;

	//		// 1. Merge Paths
	//		if (externalDocument.Paths != null)
	//			foreach (var path in externalDocument.Paths)
	//			{
	//				// Use TryAdd to ensure your application's generated paths take precedence
	//				document.Paths.TryAdd($"/api/nc/openapi/{endpoint}{path.Key}", path.Value);
	//				//foreach (var operation in path.Value.Operations)
	//				//{
	//				//	operation.Value.Tags ??= new List<OpenApiTag>() { new OpenApiTag() { Name = endpoint } };
	//				//	foreach (var tag in operation.Value.Tags.Where(t => !t.Name.StartsWith($"{endpoint}:")))
	//				//		tag.Name = $"{endpoint}:{tag.Name}";
	//				//	//if (!operation.Value.Tags.Any(t => t.Name == endpoint))
	//				//	//	operation.Value.Tags.Add(new OpenApiTag { Name = endpoint });
	//				//}
	//			}


	//		// 2. Merge Components (Schemas, Security Schemes, etc.)
	//		if (externalDocument.Components != null)
	//		{
	//			document.Components ??= new OpenApiComponents();

	//			// Merge Schemas
	//			if (externalDocument.Components.Schemas != null)
	//			{
	//				document.Components.Schemas ??= new Dictionary<string, OpenApiSchema>();
	//				foreach (var schema in externalDocument.Components.Schemas)
	//				{
	//					document.Components.Schemas.TryAdd(schema.Key, schema.Value);
	//				}
	//			}

	//			// Security Schemes
	//			if (externalDocument.Components!.SecuritySchemes != null)
	//			{
	//				document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
	//				foreach (var scheme in externalDocument.Components.SecuritySchemes)
	//					document.Components.SecuritySchemes.TryAdd(scheme.Key, scheme.Value);
	//			}

	//			// Parameters
	//			if (externalDocument.Components.Parameters != null)
	//			{
	//				document.Components.Parameters ??= new Dictionary<string, OpenApiParameter>();
	//				foreach (var param in externalDocument.Components.Parameters)
	//					document.Components.Parameters.TryAdd(param.Key, param.Value);
	//			}

	//			// Responses
	//			if (externalDocument.Components.Responses != null)
	//			{
	//				document.Components.Responses ??= new Dictionary<string, OpenApiResponse>();
	//				foreach (var resp in externalDocument.Components.Responses)
	//					document.Components.Responses.TryAdd(resp.Key, resp.Value);
	//			}

	//			// Headers
	//			if (externalDocument.Components.Headers != null)
	//			{
	//				document.Components.Headers ??= new Dictionary<string, OpenApiHeader>();
	//				foreach (var header in externalDocument.Components.Headers)
	//					document.Components.Headers.TryAdd(header.Key, header.Value);
	//			}

	//			// Examples
	//			if (externalDocument.Components.Examples != null)
	//			{
	//				document.Components.Examples ??= new Dictionary<string, OpenApiExample>();
	//				foreach (var example in externalDocument.Components.Examples)
	//					document.Components.Examples.TryAdd(example.Key, example.Value);
	//			}

	//			// RequestBodies
	//			if (externalDocument.Components.RequestBodies != null)
	//			{
	//				document.Components.RequestBodies ??= new Dictionary<string, OpenApiRequestBody>();
	//				foreach (var body in externalDocument.Components.RequestBodies)
	//					document.Components.RequestBodies.TryAdd(body.Key, body.Value);
	//			}

	//			// Links
	//			if (externalDocument.Components.Links != null)
	//			{
	//				document.Components.Links ??= new Dictionary<string, OpenApiLink>();
	//				foreach (var link in externalDocument.Components.Links)
	//					document.Components.Links.TryAdd(link.Key, link.Value);
	//			}

	//			// Callbacks
	//			if (externalDocument.Components.Callbacks != null)
	//			{
	//				document.Components.Callbacks ??= new Dictionary<string, OpenApiCallback>();
	//				foreach (var callback in externalDocument.Components.Callbacks)
	//					document.Components.Callbacks.TryAdd(callback.Key, callback.Value);
	//			}

	//			// Merge Tags
	//			if (externalDocument.Tags != null)
	//			{
	//				document.Tags ??= new List<OpenApiTag>();
	//				document.Tags = document.Tags
	//					.Concat(externalDocument.Tags)
	//					.DistinctBy(t => t.Name)
	//					.ToList();
	//			}
	//		}
	//	}
	//	return;
	//});
});


// 4. BUILD APP
var app = builder.Build();
// var options = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.OpenApi.OpenApiOptions>>().Value;

// Configure the HTTP request pipeline
app.UseStaticFiles();
app.UseRouting();
// app.UseAuthorization(); // UseAuthorization should be placed here if needed

app.MapNascachtEndpoints();
app.MapOpenApi();
// app.MapOpenApiDocument("petstore", "/api/nc/openapi/petstore");
app.MapScalarApiReference((options, context) =>
{
	options.AddDocument("nc", "Nascacht", "openapi/v1.json", true);
	options.AddDocument("petstore", "PetStore", "api/nc/openapi/petstore", true);
});

app.Run();
