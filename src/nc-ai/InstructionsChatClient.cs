using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace nc.Ai;

public sealed class InstructionsChatClient(IChatClient inner, AgentInstructions instructions)
	: DelegatingChatClient(inner)
{
	public override async Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		options = await ApplyAsync(options);
		return await base.GetResponseAsync(messages, options, cancellationToken);
	}

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
