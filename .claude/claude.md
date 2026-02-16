# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

```bash
# Build entire solution
dotnet build nc-hub.slnx

# Build a single project
dotnet build src/nc-ai/nc-ai.csproj

# Run all tests
dotnet test nc-hub.slnx

# Run tests for a single project
dotnet test tests/nc-ai-tests/nc-ai-tests.csproj

# Run a single test by name
dotnet test tests/nc-ai-tests/nc-ai-tests.csproj --filter "FullyQualifiedName~CachedChatClientTests.Passthrough_CreateCache"

# Pack NuGet packages
dotnet pack nc-hub.slnx --output ./dist
```

## Project Architecture

**Nascacht** ("connect" in Gaelic) — a suite of wrappers enabling multi-cloud architectures. Projects are published as individual NuGet packages.

### Layer Diagram

```
nc-web / nc-api                     ← ASP.NET Core host & endpoints
nc-storage                          ← Multi-cloud file storage (FluentStorage)
nc-aws / nc-azure / nc-google       ← Cloud provider implementations
nc-cloud                            ← Cloud abstractions (ITenant, ITenantManager)
nc-ai                               ← AI provider integrations (Gemini, Anthropic, OpenAI)
nc-hub / nc-data / nc-reflection    ← Core streaming, EF Core, source generators
nc-extensions / nc-scaling           ← DI utilities, TPL pipeline
nc-models                           ← Domain models
```

### Key Abstractions

- **Multi-tenant cloud**: `ITenant` / `ITenantManager<T>` / `ITenantAccessor<T>` in `nc-cloud`. Each cloud provider (AWS, Azure, Google) implements these to provide uniform access to cloud resources.
- **Storage**: `StorageService` in `nc-storage` wraps FluentStorage, routing URIs by scheme/host prefix to the correct cloud provider.
- **AI caching**: `ICacheStrategy` / `CachedChatClient` in `nc-ai/Caching/` provides provider-agnostic prompt caching. `GeminiCacheStrategy` uses server-side caching; `PassthroughCacheStrategy` for others.
- **Scaling**: `TplPipeline` in `nc-scaling` for TPL Dataflow-based processing pipelines.

### DI Registration Pattern

Each module uses partial static extension methods supporting:
- an options class
- a configuration section that calls services.Configure<{options}>
- converging to a private core method to keep the optison and config version dry


```csharp
public static partial class AiServiceExtensions
{
    public static IServiceCollection AddAiServices(this IServiceCollection services, IConfiguration configuration) { ... }
    public static IServiceCollection AddAiServices(this IServiceCollection services, AiOptions options) { ... }
    private static IServiceCollection AddAiServices(this IServiceCollection services) { ... }
}
```

## Configuration

- **User Secrets ID**: `nc-hub` (set globally in `Directory.Build.props`)
- **Config root**: `nc` section (e.g., `nc:aws:region`, `nc:azure:serviceurl`)
- **CI environment variables**: Prefixed `nc_hub__` with double-underscore as colon separator

## Code Style

- Tabs for indentation
- File-scoped namespaces (`namespace nc.Storage;`)
- Nullable reference types enabled, latest C# language version
- Private fields: `_camelCase`; async methods: `Async` suffix
- Records for options/config types with `init` accessors
- Collection expressions (`[]`, `[.. spread]`) preferred
- `DelegatingChatClient` pattern for IChatClient middleware (override both `GetResponseAsync` and `GetStreamingResponseAsync`)
- `IEnumerable<ChatMessage>` signatures in Microsoft.Extensions.AI v10.2.0

## Testing Patterns

- **Framework**: xUnit with `[Fact]` / `[Theory]`
- **Fixtures**: `IAsyncLifetime` with `Task` return types (not `ValueTask`)
- **Shared fixtures**: `[Collection(nameof(Fixture))]` + `[CollectionDefinition]`
- **Abstract base classes** for shared tests of interface implementations
- **Nested classes**: Facts file for each class being tests, and a nested class grouping each method: `public class SomeMethodAsync : SomeClassFacts`
- **Integration tests**: Testcontainers where possible, actual cloud services when there is no matching testcontainer.
- **Config in tests**: ensure compatibiltiy for both Visual Studio tests (user secrets) and Github piplines (environment variables) `ConfigurationBuilder` with `AddUserSecrets("nc-hub")` + `AddEnvironmentVariables("nc_hub__")`

## CI/CD

- GitHub Actions on push to `main` and on version tags (`v*`)
- Multi-cloud auth via OIDC (AWS, Azure, GCP Workload Identity Federation)
- LocalStack container started by CI for AWS integration tests
- NuGet publishing uses Trusted Publisher OIDC tokens
