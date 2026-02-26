using Microsoft.Extensions.AI;
using Polly;
using Polly.Retry;

namespace nc.Ai;

/// <summary>
/// A <see cref="DelegatingChatClient"/> that retries transient failures
/// (timeouts and HTTP errors) using an exponential back-off policy.
/// </summary>
internal sealed class RetryChatClient : DelegatingChatClient
{
	private readonly ResiliencePipeline _pipeline;

	public RetryChatClient(IChatClient inner, int retryCount) : base(inner)
	{
		_pipeline = new ResiliencePipelineBuilder()
			.AddRetry(new RetryStrategyOptions
			{
				MaxRetryAttempts = retryCount,
				BackoffType = DelayBackoffType.Exponential,
				UseJitter = true,
				Delay = TimeSpan.FromSeconds(2),
				ShouldHandle = new PredicateBuilder()
					.Handle<TaskCanceledException>()
					.Handle<HttpRequestException>()
			})
			.Build();
	}

	public override Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default) =>
		_pipeline.ExecuteAsync(
			ct => new ValueTask<ChatResponse>(base.GetResponseAsync(messages, options, ct)),
			cancellationToken).AsTask();
}
