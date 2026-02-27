using nc.Ai.Interfaces;

namespace nc.Ai.OpenAI;

/// <summary>
/// Configuration record for an OpenAI agent.
/// </summary>
public record OpenAIAgent : IAgent
{
	/// <summary>Gets or sets the OpenAI model identifier (e.g. <c>gpt-4o</c>, <c>o1</c>).</summary>
	public string Model { get; set; } = string.Empty;

	/// <summary>Gets or sets the OpenAI API key. Falls back to the <c>OPENAI_API_KEY</c> environment variable when <c>null</c>.</summary>
	public string? ApiKey { get; set; }

	/// <summary>
	/// Gets or sets whether to use the experimental <c>OpenAI.Responses</c> client,
	/// which supports native conversation threading via the Responses API.
	/// Defaults to <c>true</c>.
	/// </summary>
	public bool UseExperimental { get; init; } = true;
}
