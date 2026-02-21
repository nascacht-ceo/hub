# nc-ai

The `nc-ai` project provides consistency around leveraging AI models via:

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
