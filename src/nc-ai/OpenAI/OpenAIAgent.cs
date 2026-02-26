using nc.Ai.Interfaces;

namespace nc.Ai.OpenAI;

public record OpenAIAgent : IAgent
{
	public string Model { get; set; } = string.Empty;
	public string? ApiKey { get; set; }
	public bool UseExperimental { get; init; } = true;
}
