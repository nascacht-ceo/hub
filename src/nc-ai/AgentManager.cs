using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using nc.Ai.Interfaces;

namespace nc.Ai;

/// <summary>
/// Default implementation of <see cref="IAgentManager"/>.
/// Maintains a named index of agent factories and applies the configured
/// <see cref="IChatClientMiddleware"/> pipeline to each <see cref="IChatClient"/> on retrieval.
/// </summary>
internal class AgentManager : IAgentManager
{
	private readonly IServiceProvider _services;
	private readonly Dictionary<string, IChatClientFactory> _index = new();
	private readonly IReadOnlyList<IChatClientMiddleware> _middleware;
	private readonly IOptionsMonitor<AgentPipelineOptions> _pipelineOptions;

	/// <summary>
	/// Initializes the manager, resolving each registration's factory from <paramref name="services"/>.
	/// </summary>
	/// <param name="services">The application service provider used to resolve factories for dynamic agents.</param>
	/// <param name="registrations">Pre-registered agent name-to-factory pairs.</param>
	/// <param name="middleware">All registered middleware steps, in DI registration order.</param>
	/// <param name="pipelineOptions">Live pipeline configuration; re-read on every <see cref="GetChatClient"/> call.</param>
	public AgentManager(
		IServiceProvider services,
		IEnumerable<AgentRegistration> registrations,
		IEnumerable<IChatClientMiddleware> middleware,
		IOptionsMonitor<AgentPipelineOptions> pipelineOptions)
	{
		_services = services;
		_middleware = middleware.ToList();
		_pipelineOptions = pipelineOptions;
		foreach (var reg in registrations)
			_index[reg.Name] = reg.Resolve(services);
	}

	/// <inheritdoc/>
	public Task AddAgentAsync<TAgent>(TAgent agent) where TAgent : IAgent
	{
		var factory = _services.GetRequiredService<IChatClientFactory<TAgent>>();
		_index[agent.Name] = factory;
		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	public IChatClient GetChatClient(string agentName)
	{
		if (!_index.TryGetValue(agentName, out var factory))
			throw new KeyNotFoundException($"No agent registered with name '{agentName}'.");

		return ApplyPipeline(factory.GetAgent(agentName), agentName);
	}

	/// <inheritdoc/>
	public IEnumerable<string> GetAgentNames() => _index.Keys;

	/// <summary>
	/// Wraps <paramref name="client"/> with the configured middleware pipeline.
	/// When <see cref="AgentPipelineOptions.Pipeline"/> is empty, all registered middleware
	/// is applied in DI order (convention mode). Otherwise only the named steps are applied
	/// in the stated order (explicit mode). In both cases the first listed step is the outermost wrapper.
	/// </summary>
	private IChatClient ApplyPipeline(IChatClient client, string agentName)
	{
		var names = _pipelineOptions.CurrentValue.Pipeline;

		// Explicit ordering when Pipeline is configured; otherwise use all registered in DI order.
		IEnumerable<IChatClientMiddleware> ordered = names.Count > 0
			? names.Select(n => _middleware.FirstOrDefault(m => m.Name == n)).OfType<IChatClientMiddleware>()
			: _middleware;

		// Pipeline is outermost-first; build the chain inside-out by reversing.
		foreach (var m in ordered.Reverse())
			client = m.Wrap(client, agentName);

		return client;
	}
}
