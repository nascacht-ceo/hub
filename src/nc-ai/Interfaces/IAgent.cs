namespace nc.Ai.Interfaces;

/// <summary>
/// Base record for all agent configuration types.
/// Provider-specific agent records inherit from this to add connection and model settings.
/// </summary>
public record IAgent
{
	/// <summary>Gets the unique name that identifies this agent within the <see cref="IAgentManager"/>.</summary>
	public string Name { get; init; } = "";

	/// <summary>Optional system instructions pre-loaded into every request made by this agent.</summary>
	public AgentInstructions? Instructions { get; set; }
}
