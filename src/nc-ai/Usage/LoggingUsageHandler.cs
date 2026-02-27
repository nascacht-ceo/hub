using Microsoft.Extensions.Logging;
using nc.Ai.Interfaces;

namespace nc.Ai;

/// <summary>
/// Default <see cref="IUsageHandler"/> that writes token usage to the application logger at
/// <c>Information</c> level. Registered automatically by <c>AddUsageTracking()</c> when no
/// custom handler is provided.
/// </summary>
public sealed class LoggingUsageHandler(ILogger<LoggingUsageHandler> logger) : IUsageHandler
{
	/// <inheritdoc/>
	public Task HandleAsync(UsageRecord record, CancellationToken cancellationToken = default)
	{
		logger.LogInformation(
			"Usage — Model: {ModelId}, In: {InputTokens}, Out: {OutputTokens}, Total: {TotalTokens}, ConversationId: {ConversationId}",
			record.ModelId,
			record.InputTokens,
			record.OutputTokens,
			record.InputTokens + record.OutputTokens,
			record.ConversationId);
		return Task.CompletedTask;
	}
}
