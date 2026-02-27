using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nc.Ai.Interfaces;
using System.Threading.Channels;

namespace nc.Ai;

/// <summary>
/// A <see cref="BackgroundService"/> that implements <see cref="IUsageTracker"/> using a
/// bounded channel. Callers enqueue records fire-and-forget via <see cref="TrackAsync"/>;
/// records are drained sequentially by the background loop and forwarded to <see cref="IUsageHandler"/>.
/// When the channel is full, the oldest record is dropped and a warning is logged.
/// On shutdown, the writer is completed so all remaining records are processed before the host exits.
/// </summary>
public sealed class BackgroundUsageTracker : BackgroundService, IUsageTracker
{
	private readonly Channel<UsageRecord> _channel;
	private readonly IUsageHandler _handler;
	private readonly ILogger<BackgroundUsageTracker> _logger;

	/// <summary>
	/// Initializes the tracker with a bounded channel sized by <see cref="UsageTrackerOptions.ChannelCapacity"/>.
	/// </summary>
	/// <param name="handler">The handler that persists or processes each dequeued record.</param>
	/// <param name="logger">Logger for warnings and errors.</param>
	/// <param name="options">Channel capacity and other tracker settings.</param>
	public BackgroundUsageTracker(IUsageHandler handler, ILogger<BackgroundUsageTracker> logger, IOptions<UsageTrackerOptions> options)
	{
		_handler = handler;
		_logger = logger;
		var capacity = options.Value.ChannelCapacity;
		_channel = Channel.CreateBounded<UsageRecord>(new BoundedChannelOptions(capacity)
		{
			FullMode = BoundedChannelFullMode.DropOldest,
			SingleReader = true,
		});
	}

	/// <inheritdoc/>
	public ValueTask TrackAsync(UsageRecord record, CancellationToken cancellationToken = default)
	{
		if (!_channel.Writer.TryWrite(record))
			_logger.LogWarning("Usage channel full; dropping record for model {ModelId}", record.ModelId);
		return ValueTask.CompletedTask;
	}

	/// <summary>
	/// Runs the drain loop. Uses <c>CancellationToken.None</c> so remaining
	/// items are processed after <see cref="StopAsync"/> calls <c>Writer.Complete()</c>.
	/// </summary>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		// ReadAllAsync(None) so we drain remaining items when Writer.Complete() is called on shutdown
		await foreach (var record in _channel.Reader.ReadAllAsync(CancellationToken.None))
		{
			try
			{
				await _handler.HandleAsync(record, stoppingToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling usage record for model {ModelId}", record.ModelId);
			}
		}
	}

	/// <summary>
	/// Signals the channel writer to complete, then waits for the base service to stop,
	/// allowing the drain loop to finish processing queued records.
	/// </summary>
	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		_channel.Writer.Complete();
		await base.StopAsync(cancellationToken);
	}
}
