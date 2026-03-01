using Microsoft.Extensions.AI;

namespace nc.Ai.Tests;

/// <summary>
/// Wraps an <see cref="IChatClient"/> and converts "resource exhausted" quota errors
/// into xUnit test skips via <see cref="Skip.Throw"/>.
/// Requires <see cref="SkippableFactAttribute"/> on the test method.
/// </summary>
internal sealed class SkipOnResourceExhaustedClient(IChatClient inner) : DelegatingChatClient(inner)
{
	public override async Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		try
		{
			return await base.GetResponseAsync(messages, options, cancellationToken);
		}
		catch (Exception ex) when (IsResourceExhausted(ex))
		{
			throw new SkipException(ex.Message);
		}
	}

	public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		return SkipOnExhausted(base.GetStreamingResponseAsync(messages, options, cancellationToken));
	}

	private static async IAsyncEnumerable<ChatResponseUpdate> SkipOnExhausted(
		IAsyncEnumerable<ChatResponseUpdate> source)
	{
		await using var e = source.GetAsyncEnumerator();
		while (true)
		{
			bool moved;
			try
			{
				moved = await e.MoveNextAsync();
			}
			catch (Exception ex) when (IsResourceExhausted(ex))
			{
				throw new SkipException(ex.Message);
			}
			if (!moved) yield break;
			yield return e.Current;
		}
	}

	private static bool IsResourceExhausted(Exception ex)
	{
		for (var e = ex; e is not null; e = e.InnerException)
			if (e.Message.Contains("resource exhausted", StringComparison.OrdinalIgnoreCase))
				return true;
		return false;
	}
}
