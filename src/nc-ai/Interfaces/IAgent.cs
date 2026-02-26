namespace nc.Ai.Interfaces;

public record IAgent
{
	public string Name { get; init; } = "";
	public AgentInstructions? Instructions { get; set; }
}
