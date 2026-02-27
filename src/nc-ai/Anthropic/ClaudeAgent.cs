using nc.Ai.Interfaces;

namespace nc.Ai.Anthropic;

/// <summary>
/// Configuration record for an Anthropic Claude agent.
/// </summary>
public record ClaudeAgent : IAgent
{
	/// <summary>Gets or sets the Claude model identifier (e.g. <c>claude-opus-4-6</c>).</summary>
	public string Model { get; set; } = string.Empty;

	/// <summary>Gets or sets the Anthropic API key. Falls back to the <c>ANTHROPIC_API_KEY</c> environment variable when <c>null</c>.</summary>
	public string? ApiKey { get; set; }
}
