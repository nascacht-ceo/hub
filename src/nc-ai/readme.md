# nc-ai

`nc-ai` is a multi-provider AI library built on `Microsoft.Extensions.AI` (MEAI v10.2+).
It provides a common abstraction for building Generative AI applications that can span multiple LLM providers,
enabling side-by-side comparison, easy provider switching, and shared cross-cutting concerns such as
retry, usage tracking, and conversation threading.

## Supported Providers

| Provider | Class | Config section |
|---|---|---|
| Google Gemini (AI Studio + Vertex AI) | `GeminiAgent` | `nc:ai:gemini` |
| Anthropic Claude | `ClaudeAgent` | `nc:ai:anthropic` |
| OpenAI | `OpenAIAgent` | `nc:ai:openai` |
| Azure AI Foundry | `FoundryAgent` | `nc:ai:azure` |

---

## Getting Started

Register one or more agents in your DI container, then resolve `IAgentManager` to get a named `IChatClient`:

```csharp
services
    .AddAiGemini("flash", opts =>
    {
        opts.Model  = "gemini-2.0-flash";
        opts.ApiKey = configuration["gemini:apikey"];
    })
    .AddAiClaude("claude", opts =>
    {
        opts.Model  = "claude-opus-4-6";
        opts.ApiKey = configuration["claude:apikey"];
    })
    .AddAiOpenAI("gpt", opts =>
    {
        opts.Model  = "gpt-4o";
        opts.ApiKey = configuration["openai:secretkey"];
    });

var manager = services.BuildServiceProvider().GetRequiredService<IAgentManager>();
var client  = manager.GetChatClient("flash");

var response = await client.GetResponseAsync("What is the capital of France?");
Console.WriteLine(response.Text); // Paris
```

Or bind from configuration (e.g. `appsettings.json` / user secrets / environment variables):

```csharp
services.AddAiGemini("flash",   configuration.GetSection("nc:ai:gemini:flash"));
services.AddAiClaude("claude",  configuration.GetSection("nc:ai:anthropic:claude"));
services.AddAiOpenAI("gpt",     configuration.GetSection("nc:ai:openai:gpt"));
services.AddAiFoundry("phi",    configuration.GetSection("nc:ai:azure:phi"));
```

---

## Features

### Agent Management

`IAgentManager` maintains a named registry of `IChatClient` instances. Each agent is
registered by name and resolved on demand, with the configured middleware pipeline applied.

```csharp
// Retrieve a named client
IChatClient client = manager.GetChatClient("flash");

// Register a dynamic agent at runtime
await manager.AddAgentAsync(new GeminiAgent { Name = "pro", Model = "gemini-2.0-pro" });

// List all registered agent names
IEnumerable<string> names = manager.GetAgentNames();
```

---

### Agent Instructions

Inject a system prompt into every request made through an agent. Instructions can be a
static string or an async factory (e.g. loaded from a file or database at first use).

**Per-agent (registered in DI):**

```csharp
services.AddAiGemini("assistant", opts =>
{
    opts.Model        = "gemini-2.0-flash";
    opts.Instructions = "You are a helpful assistant. Always respond in markdown.";
});
```

**Per-call (wrapped at runtime):**

```csharp
AgentInstructions instructions = "You are a helpful assistant.";
// or: AgentInstructions instructions = async () => await File.ReadAllTextAsync("prompt.txt");

var client = new InstructionsChatClient(manager.GetChatClient("flash"), instructions);
var response = await client.GetResponseAsync("Summarise the news.");
```

> **Gemini context caching:** When instructions exceed 2 048 words, `GeminiChatClient`
> automatically creates a server-side cached content entry and reuses it on subsequent calls,
> reducing cost and latency.

---

### Middleware Pipeline

`AgentManager` applies a configurable chain of `IChatClientMiddleware` to every agent.
Register middleware in DI and control ordering via `AgentPipelineOptions`.

```csharp
services
    .AddRetry(opts => opts.RetryCount = 3)       // PipelineStep.Retry
    .AddUsageTracking();                          // PipelineStep.UsageTracking
```

**Convention mode** (default — empty `Pipeline` list): all registered middleware is applied
in DI registration order, outermost first.

**Explicit mode**: list only the steps you want, in the order you want them:

```csharp
services.Configure<AgentPipelineOptions>(opts =>
    opts.Pipeline = [PipelineStep.Retry, PipelineStep.UsageTracking]);
```

**Custom middleware:**

```csharp
public class LoggingMiddleware : IChatClientMiddleware
{
    public string Name => "logging";
    public IChatClient Wrap(IChatClient inner, string agentName) =>
        inner.AsBuilder().Use(next => async (messages, options, ct) =>
        {
            Console.WriteLine($"[{agentName}] Sending {messages.Count()} messages");
            return await next.GetResponseAsync(messages, options, ct);
        }).Build();
}

services.TryAddEnumerable(ServiceDescriptor.Singleton<IChatClientMiddleware, LoggingMiddleware>());
```

---

### Retry

Wraps any agent with an exponential back-off Polly pipeline that retries on
`TaskCanceledException` and `HttpRequestException`.

```csharp
// Via middleware pipeline (recommended)
services.AddRetry(opts => opts.RetryCount = 5);

// Direct wrap
var resilient = new RetryChatClient(client, retryCount: 3);
```

---

### Usage Tracking

Background, fire-and-forget token usage tracking. A bounded channel decouples the
hot path from persistence so callers are never blocked.

```csharp
// Register with the default logging handler
services.AddUsageTracking();

// Or provide a custom handler (e.g. write to a database)
services.AddUsageTracking<MyDatabaseUsageHandler>(opts => opts.ChannelCapacity = 5000);
```

Implement `IUsageHandler` to persist records:

```csharp
public class MyDatabaseUsageHandler(AppDbContext db) : IUsageHandler
{
    public async Task HandleAsync(UsageRecord record, CancellationToken ct)
    {
        db.UsageRecords.Add(new UsageEntry
        {
            ModelId        = record.ModelId,
            InputTokens    = record.InputTokens,
            OutputTokens   = record.OutputTokens,
            ConversationId = record.ConversationId,
            Agent          = record.Tags.GetValueOrDefault("agent")?.ToString(),
            Timestamp      = record.Timestamp,
        });
        await db.SaveChangesAsync(ct);
    }
}
```

`UsageRecord` is populated automatically when the middleware pipeline includes
`PipelineStep.UsageTracking`. The `agent` tag is set to the registered agent name.

Usage can also be added directly to any `IChatClient` without going through the pipeline:

```csharp
var tracked = client.WithUsageTracking(tracker, tags: new Dictionary<string, object?> { ["env"] = "prod" });
```

---

### Conversation Threading

Client-side conversation history management backed by `IDistributedCache`.
On each call, stored history is loaded, merged with the new messages, compacted if necessary,
and saved after the response arrives.

```csharp
// Register the store (falls back to in-memory cache when none is configured)
services.AddConversationThreads(opts => opts.SlidingExpiration = TimeSpan.FromHours(4));
```

```csharp
// Start a new thread
var response1 = await client.GetResponseAsync("My name is Alice.");

// Continue the thread
var response2 = await client.GetResponseAsync(
    "What is my name?",
    new ChatOptions { ConversationId = response1.ConversationId });

// response2.Text contains "Alice"
```

**Compaction:** `SlidingWindowCompactionStrategy` (default) keeps system messages and the
most recent N non-system messages. Configure the window size:

```csharp
services.Configure<AgentPipelineOptions>(...);
services.TryAddSingleton<ICompactionStrategy>(
    _ => new SlidingWindowCompactionStrategy(new SlidingWindowOptions { MaxMessages = 40 }));
```

**OpenAI native threads:** `OpenAIChatClient` (experimental Responses API) advertises
`INativeConversations`. When detected, `ConversationChatClient` bypasses the client-side
store and forwards `ConversationId` directly to the provider.

---

### File Handling & URI Content

Providers that do not accept remote URIs (Claude, Azure AI Foundry) are automatically
wrapped with `UriContentDownloader`, which downloads HTTP/HTTPS content to inline bytes
before forwarding the request.

Pass any file URI as a `UriContent`:

```csharp
var message = new ChatMessage(ChatRole.User,
[
    new UriContent("https://example.com/report.pdf", "application/pdf"),
    new TextContent("Summarise this document.")
]);
var response = await client.GetResponseAsync([message]);
```

To upload files to provider storage (e.g. Gemini Files API), use `IAiFileService<,>`:

```csharp
await geminiFileService.UploadUriAsync(new Uri("https://example.com/video.mp4"), "video/mp4", ct);
```

---

### ChatResponse Extensions

Helpers for parsing structured output from model responses:

```csharp
// Strip markdown code fences and deserialize as JSON
MyModel? result = response.Deserialize<MyModel>();

// Strip fences and parse as XML
XDocument doc = response.ToXDocument();

// Extract just the code block content (any language)
string? code = response.ExtractCode();

// Extract a specific language block
string? json = response.ExtractCode("json");
```

---

### AI Context Cache

Store and retrieve `AIContent` objects (e.g. server-side cache handles) across requests:

```csharp
services.AddSingleton<IAiContextCache, AiContextCache>();
services.Configure<AiContextCacheOptions>(opts => opts.CacheDurationMinutes = 60);
```

```csharp
await cache.SetContextAsync("my-key", content);
var content = await cache.GetContextAsync("my-key");
```

---

## Azure AI Foundry Authentication

`FoundryAgent` supports the same credential priority chain as `AzureTenant`:

| Priority | Credential |
|---|---|
| 1 | Workload Identity Federation (`FederatedTokenFile` / `FederatedToken`) |
| 2 | Client Assertion (`ClientAssertion`) |
| 3 | Certificate (`CertificatePath`) |
| 4 | Managed Identity (`UseManagedIdentity`) |
| 5 | Client Secret (`ClientSecret`) |
| 6 | `DefaultAzureCredential` (fallback) |

```csharp
services.AddAiFoundry("phi", opts =>
{
    opts.Model    = "Phi-4";
    opts.Endpoint = "https://my-project.services.ai.azure.com/models";
    opts.UseManagedIdentity = true;
});
```

---

## Configuration Reference

All agent options bind from named `IOptionsMonitor<TAgent>` entries so live reload works out of the box.

```json
{
  "nc": {
    "ai": {
      "gemini": {
        "flash": {
          "model": "gemini-2.0-flash",
          "apiKey": "...",
          "cacheTtl": "01:00:00"
        }
      },
      "anthropic": {
        "claude": { "model": "claude-opus-4-6", "apiKey": "..." }
      },
      "openai": {
        "gpt": { "model": "gpt-4o", "apiKey": "..." }
      },
      "azure": {
        "phi": {
          "model": "Phi-4",
          "endpoint": "https://my-project.services.ai.azure.com/models",
          "useManagedIdentity": true
        }
      }
    }
  }
}
```

CI environment variables use `nc_hub__` prefix with `__` as the section separator:

```
nc_hub__nc__ai__gemini__flash__apiKey=...
nc_hub__nc__ai__anthropic__claude__apiKey=...
```
