# Coding Guide

## Architect for Scalability

To ensure a product is scalable, ensure all endpoints support a streaming mode.
If one must operate on 10,000 items, 1 connection with a stream of 10,000 items is almost
always more performant than 10,000 connections with 1 item each.

To provide `ToDo List` functionality as a product, one might start with the following api endpoints:

|Endpoint|Description|
|-|-|
|GET /todo|Gets a list of items.|
|PUT /todo|Creates a new item from a Json object.|
|POST /todo/{id}|Updates an existing item.|
|GET /todo/{id}|Gets a single item.|
|DELETE /todo/{id}|Deletes a single item.|

For scalability, the endpoints above should be extended to include:

|Endpoint|Description|
|-|-|
|GET /todo?\{search}|Searches for items matching the {search} expression.|
|PUT /todo|Creates new items from a stream.|
|POST /todo|Merge items from a stream. If an item has an identifier, update it, otherwise insert it.|
|DELETE /todo/\{search}|Deletes items matching the {search} expression.|
|DELETE /todo|Deletes items in the stream.|

## Support Offline Processing

Coding bootcamp would have us create an MVC controller and implement the endpoints above.
This assumes all requests come via an HTTP request.

Instead, create a service that handles the core functionality of our endpoints,
and have the controller leverage the service.

```csharp
public interface IToDoItem {...}
public interface IToDoService
{
    // Handle all GETs
    public IEnumerable<IToDoItem> Get(SearchParameters search);
    // Handle all PUTs and POSTs
    public IEnumerable<Id> Save(IEnumerable<IToDoItem> items);
    // Handle all DELETEs
    public IEnumerable<Id> Delete(IEnumerable<Id> ids);
}
```

This way, an `IEnumeration<IToDoItem>` might be created from:

- an HTTP POST containing a Json stream
- custom logic reading spreadsheets
- gRCP or WebSocket streams

## Dependency Injection

Use dependency injection as much as possible, including:

- Registering options via `IOptions`
- Extending `IServiceCollection` with methods to simplify consuming the service

Assume we've implemented `ITodoService`:

```csharp
public ToDoService: IToDoService 
{
    // Note the use of IOptionsMonitor, to pick up in-flight configuration changes.
    public ToDoService(IOptionsMonitor<ToDoServiceOptions> options)
    { ... }
}

public ToDoServiceOptions: IValidatableObject
{
    // Make it easy to know where in configuration these options should exist.
    public const string ConfigurationPath = "nc:todo";

    ...

    // Centralize validation checking.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    { ... }
}
```

Developers should be able to inject the service from `Program.cs`:

```csharp
// If the service can be run with default options:
services.AddToDoService(options);

// Allow the consumer to define the options:
var options = new ToDoServiceOptions() {...};
services.AddToDoService(options);

// Allow the consume to configuration-drive the otions:
services.AddToDoService(configuration.GetSection(ToDoServiceOptions.ConfigurationPath);
```

An implementation of these extension methods might be:

```csharp
public static class ToDoServiceExtensions
{
    // Best option: configuration-drive options.
    public static IServiceCollection AddToDoService(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .Configure<ToDoServiceOptions>(configuration)
            .AddOpenApiService();
    }

    // Allow no options, or consumer-specified options.
    // Always inject IOptions via Options.Create(...), so they work with IServiceCollection.ConfigureOptions.
    public static IServiceCollection AddToDoService(this IServiceCollection services, OpenApiServiceOptions? options = null, ILogger? logger = null)
    {
        return services
            .AddSingleton(Options.Create(options ?? new OpenApiServiceOptions()))
            .AddOpenApiService();
    }

    // Keep the injection of required services DRY
    private static IServiceCollection AddToDoService(this IServiceCollection services)
    {
        return services.AddSingleton<IToDoSercice, ToDoService>();
    }
}
```

## Configuration

Configuration for nc-based project comprises:

```json
{
	"nc": {
		"ai": { ... },
		"aws": { ... },
		"azure": { ... },
		"cloud": { ... },
		"hub" : { ... },
		"openapi": { ... }
	}
}
```

