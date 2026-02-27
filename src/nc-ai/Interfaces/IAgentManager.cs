using Microsoft.Extensions.AI;

namespace nc.Ai.Interfaces;

/// <summary>
/// Manages a named registry of AI chat clients, optionally wrapping each one
/// with a configured middleware pipeline.
/// </summary>
public interface IAgentManager
{
	/// <summary>Returns the <see cref="IChatClient"/> for the named agent, with the current middleware pipeline applied.</summary>
	/// <param name="agentName">The name the agent was registered under.</param>
	/// <exception cref="KeyNotFoundException">No agent with <paramref name="agentName"/> is registered.</exception>
	public IChatClient GetChatClient(string agentName);

	/// <summary>
	/// Registers a dynamically-created agent at runtime using the appropriate
	/// <see cref="IChatClientFactory{TAgent}"/> resolved from the service provider.
	/// </summary>
	/// <typeparam name="TAgent">The concrete agent type, which determines which factory resolves it.</typeparam>
	/// <param name="agent">The agent configuration to register.</param>
	public Task AddAgentAsync<TAgent>(TAgent agent) where TAgent: IAgent;

	/// <summary>Returns the names of all currently registered agents.</summary>
	public IEnumerable<string> GetAgentNames();
}
