# nc-ai

The `nc-ai` provides a common approach to building rich GenerativeAI applicaiton across LLM providers.

As the major AI providers are racing to improve their models and features, 
`nc-ai` provides a common abstraction layer to enable developers to use multiple providers in the same application. 
This allows side-by-side testing of GenAI features.

|Feature|Description|
|-|-|
|Agent management|Simplfies creation of agents with common sets of instructions across your server farm.|
|Context caching|Automatically leverage context caching for providers that support it (Gemini, Claude), saving costs.|
|File handling|Enables use of UriClient across all providers.|
|Function calling|Exposes MCP endpoint wrappers around OpenAPI endpoints.|
|Pipelining|Enables chaining of multiple GenAI calls together, with support for error handling and retries.|

Based in `Microsoft.SemanticKernel`, `nc-ai` sugar includes:

|Feature | Description | 
|-|-|
|Batching| If the GenAI provider supports batching, `nc-ai` wrappers automatically batch requests together, improving throughput and reducing costs.|
|Context caching| If the GenAI provider supports context caching, `nc-ai` wrappers leverage it, saving on transaction costs.|
|File handling| Common abstractions for uploading files and using them in prompts, including support for provider-specific features like Google Gemini's file system.|

Supported providers include:

- Google
- Anthropic
- OpenAI
- Microsoft Foundary
- Amazon Bedrock

Expanding on `Microsoft.SemanticKernel`, `nc-ai` easy creation of AI agents, 
function calling, and more.

# Getting Started

```chsharp
var config = new ConfiugrationBuilder.AddJsonFile("appsettings.json").Build();
var services = new ServiceCollection().AddNascachtAiServices(config.GetSection("nc:ai")).BuildServiceProvider();

var manager = services.GetRequiredService<IAgentManager>();
await manager.AddAgentAsync(new GoogleAgent() {
    Name = "google-agent",
    Model = "gpt-4.0",
    Temperature = 0.7,
    MaxTokens = 2048
});
```
