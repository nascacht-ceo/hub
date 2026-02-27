using Microsoft.Extensions.AI;

namespace nc.Ai.Interfaces;

/// <summary>
/// Creates and caches <see cref="IChatClient"/> instances by agent name.
/// </summary>
public interface IChatClientFactory
{
	/// <summary>Returns the <see cref="IChatClient"/> for the given agent name, constructing it on first access.</summary>
	/// <param name="name">The named-options name identifying the agent configuration.</param>
	IChatClient GetAgent(string name = "");

	/// <summary>Returns the names of all agents this factory has already created.</summary>
	IEnumerable<string> GetAgentNames();
}

/// <summary>
/// Typed variant of <see cref="IChatClientFactory"/> scoped to a specific
/// <typeparamref name="TAgent"/> configuration type.
/// </summary>
/// <typeparam name="TAgent">The agent configuration record type.</typeparam>
public interface IChatClientFactory<TAgent> : IChatClientFactory where TAgent : IAgent { }
