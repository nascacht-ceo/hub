namespace nc.Ai;

/// <summary>
/// Holds system instructions for an agent, supporting both static strings and
/// asynchronous factories (e.g. loaded from a file or database at first use).
/// The result is evaluated lazily and cached for the lifetime of the instance.
/// </summary>
public sealed class AgentInstructions
{
	private readonly Lazy<Task<string>> _value;

	/// <summary>Initializes instructions from a static string.</summary>
	/// <param name="instructions">The system prompt text.</param>
	public AgentInstructions(string instructions)
		=> _value = new(() => Task.FromResult(instructions));

	/// <summary>Initializes instructions from an async factory, evaluated lazily on first call to <see cref="GetAsync"/>.</summary>
	/// <param name="factory">An async function that produces the system prompt text.</param>
	public AgentInstructions(Func<Task<string>> factory)
		=> _value = new(factory);

	/// <summary>Returns the instruction text, invoking the factory on the first call.</summary>
	public Task<string> GetAsync() => _value.Value;

	/// <summary>Implicitly wraps a string as <see cref="AgentInstructions"/>.</summary>
	public static implicit operator AgentInstructions(string instructions) => new(instructions);

	/// <summary>Implicitly wraps an async factory as <see cref="AgentInstructions"/>.</summary>
	public static implicit operator AgentInstructions(Func<Task<string>> factory) => new(factory);
}
