using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

namespace nc.Scaling;

/// <summary>
/// Enable fluid coding style to build TPL Dataflow pipelines.
/// <see href="https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/dataflow-task-parallel-library"/>
/// </summary>
public class TplPipeline: IPipeline<TplPipeline>
{
	List<TplPipelineStep> _blocks = new List<TplPipelineStep>();
	private readonly TplScalingOptions _options;
	private readonly ILogger<TplPipeline>? _logger;

	List<Func<Task>> _startMethods = new List<Func<Task>>();

	/// <summary>
	/// Initializes a new instance of the <see cref="TplPipeline"/> class,  optionally configuring scaling options
	/// and logging.
	/// </summary>
	/// <param name="options">The scaling options to configure the pipeline. If null, default scaling options are used.</param>
	/// <param name="logger">An optional logger instance for logging pipeline-related events. If null, no logging is performed.</param>
	public TplPipeline(TplScalingOptions? options = null, ILogger<TplPipeline>? logger = null)
	{
		_options = options ?? new TplScalingOptions();
		_logger = logger;
	}

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public TplPipeline From<TInput>(IAsyncEnumerable<TInput> inputs, IScalingOptions<TplPipeline>? options = null)
	{
		// _inputs = inputs;
		var buffer = new BufferBlock<TInput>(options as TplScalingOptions ?? _options);
		_logger?.LogDebug("Created BufferBlock<{Type}> as input block.", typeof(TInput).Name);
		_blocks.Add(new(buffer, typeof(TInput), typeof(TInput)));
		_startMethods.Add(async () =>
		{

			await foreach (var input in inputs)
			{
				await buffer.SendAsync(input);
			}
			buffer.Complete();
			_logger?.LogDebug("Completed feeding data to initial BufferBlock<{Type}>.", typeof(TInput).Name);
		});
		return this;
	}

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public TplPipeline Transform<TInput, TOutput>(Func<TInput, Task<TOutput>> operation, IScalingOptions<TplPipeline>? options = null)
	{
		var sourceBlock = _blocks.Where(block => block.ReturnType == typeof(TInput)).LastOrDefault()?.Block as ISourceBlock<TInput>;
		if (sourceBlock == null)
			throw new ArgumentOutOfRangeException(nameof(TInput), "The input type does not match the previous block's output type.");

		var transform = new TransformBlock<TInput, TOutput>(operation, options as TplScalingOptions ?? _options);
		sourceBlock.LinkTo(transform, new DataflowLinkOptions { PropagateCompletion = true });
		_blocks.Add(new(transform, typeof(TInput), typeof(TOutput)));
		_logger?.LogDebug("Created TransformBlock<{InputType}, {OutputType}> and linked it to previous block.", typeof(TInput).Name, typeof(TOutput).Name);
		return this;
	}

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public TplPipeline TransformMany<TInput, TOutput>(Func<TInput, IAsyncEnumerable<TOutput>> operation, IScalingOptions<TplPipeline>? options = null)
	{
		var sourceBlock = _blocks.Where(block => block.ReturnType == typeof(TInput)).LastOrDefault()?.Block as ISourceBlock<TInput>;
		if (sourceBlock == null)
			throw new ArgumentOutOfRangeException(nameof(TInput), "The input type does not match the previous block's output type.");

		var transformMany = new TransformManyBlock<TInput, TOutput>(operation, options as TplScalingOptions ?? _options);
		sourceBlock.LinkTo(transformMany, new DataflowLinkOptions { PropagateCompletion = true });
		_blocks.Add(new(transformMany, typeof(TInput), typeof(TOutput)));
		return this;
	}
	
	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public TplPipeline Batch<TInput>(int batchSize, IScalingOptions<TplPipeline>? options = null)
	{
		var sourceBlock = _blocks.Where(block => block.ReturnType == typeof(TInput)).LastOrDefault()?.Block as ISourceBlock<TInput>;
		if (sourceBlock == null)
			throw new ArgumentOutOfRangeException(nameof(TInput), "No previous blocks of type {TInput} found.", typeof(TInput).Name);

		var batch = new BatchBlock<TInput>(batchSize, options as TplScalingOptions ?? _options);
		sourceBlock.LinkTo(batch, new DataflowLinkOptions { PropagateCompletion = true });
		_blocks.Add(new(batch, typeof(TInput), typeof(IEnumerable<TInput>)));
		_logger?.LogDebug("Created BatchBlock<{InputType}> with a batch size of {batchSize}.", typeof(TInput).Name, batchSize);
		return this;
	}
	
	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public TplPipeline Join<TInputA, TInputB>(IScalingOptions<TplPipeline>? options = null)
	{
		var inputBBlock = _blocks.Where(block => block.ReturnType == typeof(TInputB)).LastOrDefault()?.Block as ISourceBlock<TInputB>;
		if (inputBBlock == null)
			throw new ArgumentOutOfRangeException(nameof(TInputB), "No previous blocks of type {TInput} found.", typeof(TInputB).Name);

		var inputABlock = _blocks.Where(block => block.ReturnType == typeof(TInputA) && block.Block != inputBBlock).LastOrDefault()?.Block as ISourceBlock<TInputA>;
		if (inputABlock == null)
			throw new ArgumentOutOfRangeException(nameof(TInputA), "No previous blocks of type {TInput} found.", typeof(TInputA).Name);

		var joinBlock = new JoinBlock<TInputA, TInputB>(options as TplScalingOptions ?? _options);
		inputABlock.LinkTo(joinBlock.Target1, new DataflowLinkOptions { PropagateCompletion = true });
		inputBBlock.LinkTo(joinBlock.Target2, new DataflowLinkOptions { PropagateCompletion = true });

		_blocks.Add(new(joinBlock, null, typeof(Tuple<TInputA, TInputB>)));
		_logger?.LogDebug("Created JoinBlock<{Target1}, {Target2}>.", typeof(TInputA).Name, typeof(TInputB).Name);
		return this;
	}
	
	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public TplPipeline Act<TInput>(Func<TInput, Task> action, IScalingOptions<TplPipeline>? options = null)
	{
		var sourceBlock = _blocks.Where(block => block.ReturnType == typeof(TInput)).LastOrDefault()?.Block as ISourceBlock<TInput>;
		if (sourceBlock == null)
			throw new ArgumentOutOfRangeException(nameof(TInput), "No previous blocks of type {TInput} found.", typeof(TInput).Name);

		var actionBlock = new ActionBlock<TInput>(action, options as TplScalingOptions ?? _options);
		sourceBlock.LinkTo(actionBlock, new DataflowLinkOptions { PropagateCompletion = true });
		_blocks.Add(new(actionBlock, typeof(TInput), null));
		_logger?.LogDebug("Created ActionBlock<{InputType}>.", typeof(TInput).Name);
		return this;
	}
	
	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public async IAsyncEnumerable<TReturn> ExecuteAsync<TReturn>(IScalingOptions<TplPipeline>? options = null)
	{
		options ??= _options;
		if (_blocks.Count == 0)
			throw new InvalidOperationException("No blocks defined in the pipeline.");
		if (_startMethods.Count == 0)
			throw new InvalidOperationException("You must call From() to define the input source before executing the pipeline.");

		_logger?.LogDebug("Starting pipeline execution.");
		await Task.WhenAll(_startMethods.Select(m => m()));
		_logger?.LogDebug("Pipeline started. Retrieving results.");
		var sourceBlock = _blocks.Where(block => block.ReturnType == typeof(TReturn)).LastOrDefault()?.Block as ISourceBlock<TReturn>;
		if (sourceBlock == null)
			throw new ArgumentOutOfRangeException(nameof(TReturn), "No previous blocks of type {TInput} found.", typeof(TReturn).Name);

		while (await sourceBlock.OutputAvailableAsync(options.CancellationToken))
		{
			if (options.CancellationToken.IsCancellationRequested)
				break;
			if (sourceBlock.Completion.IsCompleted)
				break;
			var result = await sourceBlock.ReceiveAsync(options.CancellationToken);
			_logger?.LogDebug("Yielding result");
			yield return result;
		}
		await sourceBlock.Completion;
		await Task.WhenAll(_blocks.Select(async b => {
			await b.Block.Completion;
			_logger?.LogDebug("Block of type {Type} to complete.", b.Block.GetType().Name);
		}));
		_logger?.LogDebug("Pipeline execution completed.");
	}

	/// <summary>
	/// Removes duplicate items from the dataflow pipeline for the specified input type.
	/// </summary>
	/// <remarks>This method ensures that only unique items of the specified input type are passed through the
	/// pipeline. Duplicate items are filtered out based on their equality as determined by <see
	/// cref="HashSet{T}"/>.</remarks>
	/// <typeparam name="TInput">The type of the input data to process.</typeparam>
	/// <param name="options">Optional scaling options that configure the behavior of the underlying dataflow block. If not specified, the
	/// default options for the pipeline are used.</param>
	/// <returns>The current <see cref="TplPipeline"/> instance, allowing for method chaining.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if the specified input type <typeparamref name="TInput"/> does not match the output type of the previous
	/// block in the pipeline.</exception>
	public TplPipeline RemoveDuplicates<TInput>(TplScalingOptions? options = null) 
	{
		var sourceBlock = _blocks.Where(block => block.ReturnType == typeof(TInput)).LastOrDefault()?.Block as ISourceBlock<TInput>;
		if (sourceBlock == null)
			throw new ArgumentOutOfRangeException(nameof(TInput), "The input type does not match the previous block's output type.");
		var seen = new HashSet<TInput>();
		var transformMany = new TransformManyBlock<TInput, TInput>(input => RemoveDuplicates(input, seen), options ?? _options);
		// var transformMany = new TransformManyBlock<TInput, IEnumerable<TInput>>(_ => new HashSet<TInput>(_), options ?? _options);

		sourceBlock.LinkTo(transformMany, new DataflowLinkOptions { PropagateCompletion = true });
		_blocks.Add(new(transformMany, typeof(TInput), typeof(TInput)));
		return this;
	}

	/// <summary>
	/// Filters out duplicate elements from the input sequence based on a provided set of already-seen elements.
	/// </summary>
	/// <remarks>This method ensures that only unique elements, as determined by the <see cref="HashSet{T}"/>, are
	/// included in the output. The caller is responsible for providing and maintaining the <paramref name="seen"/>
	/// set.</remarks>
	/// <typeparam name="TInput">The type of the elements in the sequence.</typeparam>
	/// <param name="input">The element to evaluate for uniqueness.</param>
	/// <param name="seen">A set containing elements that have already been encountered. This set is updated with new elements as they are
	/// processed.</param>
	/// <returns>An asynchronous stream that yields the input element if it is not a duplicate.</returns>
	private static async IAsyncEnumerable<TInput> RemoveDuplicates<TInput>(TInput input, HashSet<TInput> seen)
	{
		if (seen.Add(input))
			yield return input;
		await Task.CompletedTask;
	}

	/// <summary>
	/// Represents a step in a TPL (Task Parallel Library) dataflow pipeline, encapsulating a dataflow block and its
	/// associated input and output types.
	/// </summary>
	/// <remarks>This class is used to define a single step in a dataflow pipeline, where each step processes data
	/// using the specified <see cref="IDataflowBlock"/>. The optional <see cref="InputType"/> and <see cref="ReturnType"/>
	/// properties can be used to specify the expected input and output types for the step, providing additional metadata
	/// for pipeline configuration or validation.</remarks>
	private class TplPipelineStep
	{
		public IDataflowBlock Block { get; set; }
		public Type? InputType { get; set; }
		public Type? ReturnType { get; set; }

		public TplPipelineStep(IDataflowBlock block, Type? inputType = null, Type? returnType = null)
		{
			Block = block;
			InputType = inputType;
			ReturnType = returnType;
		}
	}
}

