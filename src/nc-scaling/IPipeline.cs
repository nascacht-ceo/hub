using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;


namespace nc.Scaling;

/// <summary>
/// Sugar for managing parallism in a data processing pipeline.
/// </summary>
public interface IPipeline
{

}

public interface IPipeline<TImplementation> : IPipeline where TImplementation: IPipeline<TImplementation>
{
	/// <summary>
	/// Creates a new pipeline builder from the specified asynchronous input sequence.
	/// </summary>
	/// <typeparam name="TInput">The type of elements in the input sequence.</typeparam>
	/// <param name="inputs">An asynchronous enumerable representing the input sequence for the pipeline. Cannot be <see langword="null"/>.</param>
	/// <param name="options">Optional scaling options to configure the pipeline's behavior. If <see langword="null"/>, default scaling options
	/// are used.</param>
	/// <returns>An <see cref="IPipeline"/> instance configured to process the specified input sequence.</returns>
	public TImplementation From<TInput>(IAsyncEnumerable<TInput> inputs, IScalingOptions<TImplementation>? options = null);

	/// <summary>
	/// Adds a transformation step to the pipeline, converting input of type <typeparamref name="TInput"/>  to output of
	/// type <typeparamref name="TOutput"/> using the specified asynchronous operation.
	/// </summary>
	/// <remarks>This method is typically used to define a step in a data processing pipeline where input data is 
	/// transformed into a different format or type. The transformation is performed asynchronously, allowing  for
	/// non-blocking operations such as I/O or network calls.</remarks>
	/// <typeparam name="TInput">The type of the input data for the transformation.</typeparam>
	/// <typeparam name="TOutput">The type of the output data produced by the transformation.</typeparam>
	/// <param name="operation">A function that defines the transformation logic. The function takes an input of type <typeparamref name="TInput"/>
	/// and returns a <see cref="Task{TResult}"/> that produces an output of type <typeparamref name="TOutput"/>.</param>
	/// <param name="options">Optional scaling options that configure how the transformation step is executed, such as concurrency limits.  If
	/// <see langword="null"/>, default scaling options are applied.</param>
	/// <returns>An <see cref="IPipeline"/> instance that can be used to further configure the pipeline.</returns>
	public TImplementation Transform<TInput, TOutput>(Func<TInput, Task<TOutput>> operation, IScalingOptions<TImplementation>? options = null);

	/// <summary>
	/// Transforms each input element into zero or more output elements by applying the specified asynchronous operation.
	/// </summary>
	/// <remarks>This method is typically used to perform many-to-many transformations where each input element can
	/// produce multiple output elements asynchronously. The transformation is applied in a streaming fashion, and the
	/// degree of parallelism can be configured using the <paramref name="options"/> parameter.</remarks>
	/// <typeparam name="TInput">The type of the input elements.</typeparam>
	/// <typeparam name="TOutput">The type of the output elements.</typeparam>
	/// <param name="operation">A function that takes an input element of type <typeparamref name="TInput"/> and returns an asynchronous enumerable
	/// of output elements of type <typeparamref name="TOutput"/>.</param>
	/// <param name="options">Optional scaling options that control the degree of parallelism and other execution settings for the
	/// transformation. If <c>null</c>, default scaling options are used.</param>
	/// <returns>An <see cref="IPipeline"/> that represents the pipeline with the transformation applied.</returns>
	public TImplementation TransformMany<TInput, TOutput>(Func<TInput, IAsyncEnumerable<TOutput>> operation, IScalingOptions<TImplementation>? options = null);

	/// <summary>
	/// Configures the pipeline to process input items in batches of a specified size.
	/// </summary>
	/// <remarks>This method enables batch processing within the pipeline, which can improve performance by reducing
	/// the overhead of processing individual items. The behavior of the batch processing can be further customized using
	/// the <paramref name="options"/> parameter.</remarks>
	/// <typeparam name="TInput">The type of the input items to be batched.</typeparam>
	/// <param name="batchSize">The number of items to include in each batch. Must be greater than zero.</param>
	/// <param name="options">Optional scaling options to control the behavior of batch processing, such as concurrency or resource limits. If
	/// not specified, default scaling options will be used.</param>
	/// <returns>An <see cref="IPipeline"/> instance configured to process input items in batches.</returns>
	public TImplementation Batch<TInput>(int batchSize, IScalingOptions<TImplementation>? options = null);

	/// <summary>
	/// Combines two input pipelines into a single pipeline that processes both inputs concurrently.
	/// </summary>
	/// <remarks>This method is typically used to merge two independent data streams into a single processing
	/// pipeline. The resulting pipeline can be configured further to define how the combined data is processed.</remarks>
	/// <typeparam name="TInputA">The type of the first input pipeline's data.</typeparam>
	/// <typeparam name="TInputB">The type of the second input pipeline's data.</typeparam>
	/// <param name="options">Optional scaling options that configure how the combined pipeline handles concurrency and resource allocation. If
	/// <see langword="null"/>, default scaling options are applied.</param>
	/// <returns>An <see cref="IPipeline"/> that represents the combined pipeline, allowing further configuration or
	/// execution.</returns>
	public TImplementation Join<TInputA, TInputB>(IScalingOptions<TImplementation>? options = null);

	/// <summary>
	/// Adds an action to the pipeline that processes input of type <typeparamref name="TInput"/> asynchronously.
	/// </summary>
	/// <typeparam name="TInput">The type of input that the action will process.</typeparam>
	/// <param name="action">A delegate representing the asynchronous action to perform on the input.  The action is invoked for each item of
	/// type <typeparamref name="TInput"/> in the pipeline.</param>
	/// <param name="options">Optional scaling options that configure how the action is executed, such as concurrency limits or other scaling
	/// behaviors.  If <c>null</c>, default scaling options are applied.</param>
	/// <returns>The current instance of <see cref="IPipeline"/>, allowing for method chaining.</returns>
	public TImplementation Act<TInput>(Func<TInput, Task> action, IScalingOptions<TImplementation>? options = null);


	/// <summary>
	/// Executes an asynchronous operation that produces a sequence of results of the specified type.
	/// </summary>
	/// <remarks>The returned sequence is lazily evaluated, meaning the operation is executed as the caller iterates
	/// through the results.</remarks>
	/// <typeparam name="TReturn">The type of the elements in the resulting sequence.</typeparam>
	/// <param name="options">Optional scaling options that influence the execution behavior. If <see langword="null"/>, default scaling behavior
	/// is applied.</param>
	/// <returns>An asynchronous enumerable sequence of <typeparamref name="TReturn"/> representing the results of the operation.</returns>
	public IAsyncEnumerable<TReturn> ExecuteAsync<TReturn>(IScalingOptions<TImplementation>? options = null);
}

public static class IPipelineBuilderExtensions
{
	/// <summary>
	/// Configures the pipeline to process the specified input collection with optional scaling options.
	/// </summary>
	/// <remarks>This method converts the input collection to an asynchronous enumerable and configures the pipeline
	/// to process it.</remarks>
	/// <typeparam name="TImplementation">The type of the pipeline builder implementation. Must implement <see cref="IPipeline{TImplementation}"/>.</typeparam>
	/// <typeparam name="TInput">The type of the elements in the input collection.</typeparam>
	/// <param name="pipeline">The pipeline builder instance to configure.</param>
	/// <param name="inputs">The collection of input elements to be processed by the pipeline.</param>
	/// <param name="options">Optional scaling options to control the behavior of the pipeline during processing. If <see langword="null"/>,
	/// default scaling options are used.</param>
	/// <returns>The configured pipeline builder instance.</returns>
	public static TImplementation From<TImplementation, TInput>(this IPipeline<TImplementation> pipeline, IEnumerable<TInput> inputs, IScalingOptions<TImplementation>? options = null)
		where TImplementation : IPipeline<TImplementation>
		=> pipeline.From(inputs.ToAsyncEnumerable(), options);

	/// <summary>
	/// Adds a transformation step to the pipeline, applying the specified operation to input values.
	/// </summary>
	/// <remarks>This method allows you to add a synchronous transformation operation to the pipeline.  The
	/// transformation is applied to each input value, producing a corresponding output value.</remarks>
	/// <typeparam name="TImplementation">The type of the pipeline builder implementation.</typeparam>
	/// <typeparam name="TInput">The type of the input values to the transformation.</typeparam>
	/// <typeparam name="TOutput">The type of the output values produced by the transformation.</typeparam>
	/// <param name="pipeline">The pipeline builder to which the transformation step is added.</param>
	/// <param name="operation">A function that defines the transformation to apply to input values.</param>
	/// <param name="options">Optional scaling options to configure the transformation step. If null, default options are used.</param>
	/// <returns>The pipeline builder with the added transformation step.</returns>
	public static TImplementation Transform<TImplementation, TInput, TOutput>(this IPipeline<TImplementation> pipeline, Func<TInput, TOutput> operation, IScalingOptions<TImplementation>? options = null)
		where TImplementation : IPipeline<TImplementation>
		=> pipeline.Transform<TInput, TOutput>(ScalingServiceExtensions.WrapSync(operation), options);

	/// <summary>
	/// Applies a transformation to each input element, producing zero or more output elements for each input.
	/// </summary>
	/// <remarks>This method enables scenarios where a single input element can produce multiple output elements,
	/// such as expanding a collection or mapping an input to multiple results.</remarks>
	/// <typeparam name="TImplementation">The type of the pipeline builder implementing <see cref="IPipeline{TImplementation}"/>.</typeparam>
	/// <typeparam name="TInput">The type of the input elements to the transformation.</typeparam>
	/// <typeparam name="TOutput">The type of the output elements produced by the transformation.</typeparam>
	/// <param name="pipeline">The pipeline builder to which the transformation is applied.</param>
	/// <param name="operation">A function that takes an input element of type <typeparamref name="TInput"/> and returns a collection of output
	/// elements of type <typeparamref name="TOutput"/>.</param>
	/// <param name="options">Optional scaling options that configure how the transformation is executed, such as concurrency settings. If null,
	/// default options are used.</param>
	/// <returns>The pipeline builder of type <typeparamref name="TImplementation"/>, allowing for further configuration or chaining
	/// of operations.</returns>
	public static TImplementation TransformMany<TImplementation, TInput, TOutput>(this IPipeline<TImplementation> pipeline, Func<TInput, IEnumerable<TOutput>> operation, IScalingOptions<TImplementation>? options = null)
		where TImplementation : IPipeline<TImplementation>
		=> pipeline.TransformMany(WrapSync(operation), options);

	/// <summary>
	/// Adds a synchronous action to the pipeline that processes input of the specified type.
	/// </summary>
	/// <typeparam name="TImplementation">The type of the pipeline builder implementation.</typeparam>
	/// <typeparam name="TInput">The type of input that the action processes.</typeparam>
	/// <param name="pipeline">The pipeline builder to which the action is added.</param>
	/// <param name="action">The action to execute for each input of type <typeparamref name="TInput"/>.</param>
	/// <param name="options">Optional scaling options that configure how the action is executed in the pipeline. If null, default options are
	/// used.</param>
	/// <returns>The same pipeline builder instance, allowing for method chaining.</returns>
	public static TImplementation Act<TImplementation, TInput>(this IPipeline<TImplementation> pipeline, Action<TInput> action, IScalingOptions<TImplementation>? options = null)
		where TImplementation : IPipeline<TImplementation>
		=> pipeline.Act<TInput>(WrapSync(action), options);

	/// <summary>
	/// Wraps a synchronous operation that produces an <see cref="IEnumerable{T}"/> into an asynchronous operation that
	/// produces an <see cref="IAsyncEnumerable{T}"/>.
	/// </summary>
	/// <remarks>This method is useful for adapting existing synchronous operations to asynchronous workflows,
	/// particularly in scenarios where asynchronous streaming is required. The returned function uses <see
	/// cref="System.Linq.AsyncEnumerable.ToAsyncEnumerable{TSource}(IEnumerable{TSource})"/> to convert the synchronous
	/// enumerable to an asynchronous enumerable.</remarks>
	/// <typeparam name="TInput">The type of the input parameter for the operation.</typeparam>
	/// <typeparam name="TOutput">The type of the elements produced by the operation.</typeparam>
	/// <param name="operation">A synchronous function that takes an input of type <typeparamref name="TInput"/> and returns an <see
	/// cref="IEnumerable{TOutput}"/>.</param>
	/// <returns>A function that takes an input of type <typeparamref name="TInput"/> and returns an <see
	/// cref="IAsyncEnumerable{TOutput}"/> representing the asynchronous version of the operation.</returns>
	public static Func<TInput, IAsyncEnumerable<TOutput>> WrapSync<TInput, TOutput>(Func<TInput, IEnumerable<TOutput>> operation)
	=> input => operation(input).ToAsyncEnumerable();

	/// <summary>
	/// Wraps a synchronous <see cref="Action{T}"/> delegate in an asynchronous <see cref="Func{TInput, Task}"/> delegate.
	/// </summary>
	/// <remarks>This method is useful for integrating synchronous code into asynchronous workflows by wrapping a
	/// synchronous action in a delegate that conforms to asynchronous patterns.</remarks>
	/// <typeparam name="TInput">The type of the input parameter for the action.</typeparam>
	/// <param name="action">The synchronous action to be wrapped. Cannot be <see langword="null"/>.</param>
	/// <returns>A <see cref="Func{TInput, Task}"/> delegate that executes the provided synchronous action and returns a completed
	/// task.</returns>
	public static Func<TInput, Task> WrapSync<TInput>(Action<TInput> action)
		=> input => 
		{
			action(input);
			return Task.CompletedTask;
		};
}