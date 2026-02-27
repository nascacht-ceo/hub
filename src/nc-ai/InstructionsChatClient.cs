using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace nc.Ai;

/// <summary>
/// A <see cref="DelegatingChatClient"/> that injects system instructions into every request.
/// Instructions are resolved asynchronously from <see cref="AgentInstructions"/> and set on
/// <c>ChatOptions.Instructions</c> before forwarding to the inner client.
/// </summary>
public sealed class InstructionsChatClient(IChatClient inner, AgentInstructions instructions)
	: DelegatingChatClient(inner)
{
	/// <inheritdoc/>
	public override async Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		options = await ApplyAsync(options);
		return await base.GetResponseAsync(messages, options, cancellationToken);
	}

	/// <inheritdoc/>
	public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		options = await ApplyAsync(options);
		await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
			yield return update;
	}

	private async Task<ChatOptions> ApplyAsync(ChatOptions? options)
	{
		var text = await instructions.GetAsync();
		options = (options ?? new()).Clone();
		options.Instructions = text;
		return options;
	}
}
