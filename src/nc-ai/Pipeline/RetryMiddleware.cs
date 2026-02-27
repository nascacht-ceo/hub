using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using nc.Ai.Interfaces;

namespace nc.Ai;

/// <summary>
/// <see cref="IChatClientMiddleware"/> adapter that wraps an agent's client with
/// <see cref="RetryChatClient"/> using the configured <see cref="RetryOptions.RetryCount"/>.
/// Register via <c>AddRetry()</c>; reference in the pipeline as <see cref="PipelineStep.Retry"/>.
/// </summary>
internal sealed class RetryMiddleware(IOptions<RetryOptions> options) : IChatClientMiddleware
{
	/// <inheritdoc/>
	public string Name => PipelineStep.Retry;

	/// <inheritdoc/>
	public IChatClient Wrap(IChatClient inner, string agentName) =>
		new RetryChatClient(inner, options.Value.RetryCount);
}
