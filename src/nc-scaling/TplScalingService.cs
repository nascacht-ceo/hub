using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks.Dataflow;
using System.Linq;

namespace nc.Scaling;

/// <summary>
/// Provides a scalable, task-parallel processing service that executes asynchronous operations  on a collection of
/// inputs, leveraging the Dataflow library for efficient parallelism.
/// </summary>
/// <remarks>This service is designed to process a potentially large number of input items concurrently,  applying
/// a user-defined asynchronous operation to each item. It supports configurable scaling  options, such as the maximum
/// degree of parallelism and cancellation tokens, to allow fine-grained  control over execution behavior.   The service
/// is thread-safe and can be used in high-concurrency scenarios. Logging is supported  to trace execution and
/// cancellation events.</remarks>
public class TplScalingService : IScalingService
{
	private readonly ILogger<TplScalingService>? _logger;
	private TplScalingServiceOptions _options;

	/// <summary>
	/// Initializes a new instance of the <see cref="TplScalingService"/> class, optionally configuring it with the
	/// specified options and logger.
	/// </summary>
	/// <remarks>The <paramref name="options"/> parameter allows dynamic updates to the service's configuration.
	/// Changes to the monitored options will automatically update the service's behavior at runtime.</remarks>
	/// <param name="options">An <see cref="IOptionsMonitor{T}"/> instance used to monitor and retrieve <see cref="TplScalingServiceOptions"/>.
	/// If null, default options are used.</param>
	/// <param name="logger">An <see cref="ILogger{T}"/> instance used for logging diagnostic messages. If null, no logging is performed.</param>
	public TplScalingService(IOptionsMonitor<TplScalingServiceOptions>? options = null, ILogger<TplScalingService>? logger = null)
	{
		_options = options?.CurrentValue ?? new TplScalingServiceOptions();
		options?.OnChange(o => _options = o);
		_logger = logger;
	}

	/// <summary>
	/// Executes an asynchronous operation on a sequence of input elements, producing a sequence of results.
	/// </summary>
	/// <remarks>This method processes the input sequence in parallel, using the specified scaling options to
	/// control concurrency. The method ensures that all input elements are processed, and the results are yielded in the
	/// order they are produced. If the operation is canceled via the <see cref="TplScalingOptions.CancellationToken"/>, the
	/// method will stop processing and yield no further results.</remarks>
	/// <typeparam name="TInput">The type of the input elements.</typeparam>
	/// <typeparam name="TReturn">The type of the output elements.</typeparam>
	/// <param name="inputs">An asynchronous sequence of input elements to process.</param>
	/// <param name="operation">A delegate that defines the asynchronous operation to apply to each input element. The delegate takes an input of
	/// type <typeparamref name="TInput"/> and returns a <see cref="Task{TReturn}"/>.</param>
	/// <param name="options">Optional scaling options that configure the degree of parallelism, task scheduling, and cancellation behavior. If
	/// not provided, default options will be used.</param>
	/// <returns>An asynchronous sequence of results of type <typeparamref name="TReturn"/> produced by applying the operation to
	/// each input element.</returns>
	public async IAsyncEnumerable<TReturn> ExecuteAsync<TInput, TReturn>(IAsyncEnumerable<TInput> inputs, Func<TInput, Task<TReturn>> operation, TplScalingOptions? options = null)
	{
		var cts = new CancellationTokenSource();
		options ??= _options.ToScalingOptions(cts);

		var transformer = new TransformBlock<TInput, TReturn>(operation, options);
		await foreach (var input in inputs)
			transformer.Post(input);

		transformer.Complete();
		while (await transformer.OutputAvailableAsync(options.CancellationToken))
		{
			if (options.CancellationToken.IsCancellationRequested)
			{
				_logger?.LogTrace("TplScalingService.ExecuteAsync cancelled.");
				break;
			}
			if (transformer.Completion.IsCompleted)
				break;
			var result = await transformer.ReceiveAsync(options.CancellationToken);
			yield return result;
		}
		await transformer.Completion;
	}

}
