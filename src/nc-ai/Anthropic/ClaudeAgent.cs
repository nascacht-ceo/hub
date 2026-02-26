using nc.Ai.Interfaces;

namespace nc.Ai.Anthropic;

public record ClaudeAgent : IAgent
{
	public string Model { get; set; } = string.Empty;
	public string? ApiKey { get; set; }
}
