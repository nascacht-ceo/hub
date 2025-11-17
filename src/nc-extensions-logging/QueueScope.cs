using System.Collections.Concurrent;

namespace nc.Extensions.Logging;

public class QueueScope : IDisposable
{
	public object? State { get; }

	public readonly BlockingCollection<QueueMessage> Queue;
	private readonly Action? _disposeCallback;

	public QueueScope(object? state = null, ConcurrentQueue<QueueMessage>? queue = null, int maxSize = 1000, Action? disposeCallback = null)
	{
		State = state;
		Queue = new BlockingCollection<QueueMessage>(queue ?? new(), maxSize);
		_disposeCallback = disposeCallback;
	}


	public void Dispose()
	{
		Queue.CompleteAdding();
		_disposeCallback?.Invoke();
	}

	public override string? ToString()
	{
		return State?.ToString();
	}
}
