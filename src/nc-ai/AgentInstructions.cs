namespace nc.Ai;

public sealed class AgentInstructions
{
	private readonly Lazy<Task<string>> _value;

	public AgentInstructions(string instructions)
		=> _value = new(() => Task.FromResult(instructions));

	public AgentInstructions(Func<Task<string>> factory)
		=> _value = new(factory);

	public Task<string> GetAsync() => _value.Value;

	public static implicit operator AgentInstructions(string instructions) => new(instructions);
	public static implicit operator AgentInstructions(Func<Task<string>> factory) => new(factory);
}
