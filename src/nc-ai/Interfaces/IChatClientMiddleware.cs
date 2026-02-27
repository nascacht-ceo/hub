using Microsoft.Extensions.AI;

namespace nc.Ai.Interfaces;

/// <summary>
/// Represents a named step in the <see cref="AgentManager"/> middleware pipeline.
/// Implementations wrap an <see cref="IChatClient"/> with cross-cutting behaviour
/// such as retry or usage tracking.
/// </summary>
public interface IChatClientMiddleware
{
	/// <summary>Gets the name used to reference this step in <see cref="AgentPipelineOptions.Pipeline"/>.</summary>
	string Name { get; }

	/// <summary>Wraps <paramref name="inner"/> with this middleware's behaviour and returns the decorated client.</summary>
	/// <param name="inner">The client to decorate.</param>
	/// <param name="agentName">The name of the agent being built; may be used for tagging or logging.</param>
	IChatClient Wrap(IChatClient inner, string agentName);
}
