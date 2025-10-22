using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Scaling;

/// <summary>
/// Provides functionality to execute operations on a collection of inputs concurrently,  with optional scaling and
/// concurrency control.
/// </summary>
/// <remarks>This method allows for concurrent processing of input elements, making it suitable for scenarios 
/// where operations can be parallelized to improve performance. The caller can control the level of  concurrency and
/// other scaling behaviors through the <paramref name="options"/> parameter.</remarks>
public interface IScalingService
{
	IAsyncEnumerable<TReturn> ExecuteAsync<TInput, TReturn>(IAsyncEnumerable<TInput> inputs, Func<TInput, Task<TReturn>> operation, TplScalingOptions? options = default);
}

/// <summary>
/// Provides extension methods for executing scalable operations using an <see cref="IScalingService"/>.
/// </summary>
/// <remarks>This class contains methods that enable efficient processing of collections or sequences of input
/// items by leveraging the scaling capabilities of an <see cref="IScalingService"/>. These methods support both
/// synchronous and asynchronous operations, and allow for optional configuration of scaling behavior through <see
/// cref="TplScalingOptions"/>.</remarks>
public static class ScalingServiceExtensions
{
	/// <summary>
	/// Executes the specified operation on a collection of input items asynchronously,  leveraging the scaling
	/// capabilities of the provided <see cref="IScalingService"/>.
	/// </summary>
	/// <remarks>This method processes the input collection in a scalable and asynchronous manner,  making it
	/// suitable for scenarios involving large datasets or operations that benefit from parallelism.</remarks>
	/// <typeparam name="TInput">The type of the input items.</typeparam>
	/// <typeparam name="TReturn">The type of the result produced by the operation.</typeparam>
	/// <param name="service">The <see cref="IScalingService"/> used to manage and scale the execution of the operation.</param>
	/// <param name="inputs">The collection of input items to process.</param>
	/// <param name="operation">A function that defines the operation to perform on each input item.</param>
	/// <param name="options">Optional scaling configuration settings that control the behavior of the execution,  such as concurrency limits or
	/// retry policies. If not provided, default scaling options are used.</param>
	/// <returns>An asynchronous stream of results, where each result corresponds to the output of the operation  applied to an
	/// input item.</returns>
	public static IAsyncEnumerable<TReturn> ExecuteAsync<TInput, TReturn>(this IScalingService service, IEnumerable<TInput> inputs, Func<TInput, TReturn> operation, TplScalingOptions? options = null)
		=> service.ExecuteAsync(inputs.ToAsyncEnumerable(), operation, options);

	/// <summary>
	/// Executes an asynchronous operation on a collection of input items, scaling the execution based on the provided
	/// options.
	/// </summary>
	/// <typeparam name="TInput">The type of the input items.</typeparam>
	/// <typeparam name="TReturn">The type of the result produced by the operation.</typeparam>
	/// <param name="service">The scaling service used to manage the execution of the operation.</param>
	/// <param name="inputs">The collection of input items to process.</param>
	/// <param name="operation">The asynchronous operation to execute for each input item.</param>
	/// <param name="options">Optional scaling options that configure the execution behavior, such as concurrency limits. If <see
	/// langword="null"/>, default scaling options are used.</param>
	/// <returns>An asynchronous stream of results, where each result corresponds to the output of the <paramref name="operation"/>
	/// applied to an input item.</returns>
	public static IAsyncEnumerable<TReturn> ExecuteAsync<TInput, TReturn>(this IScalingService service, IEnumerable<TInput> inputs, Func<TInput, Task<TReturn>> operation, TplScalingOptions? options = null)
		=> service.ExecuteAsync(inputs.ToAsyncEnumerable(), operation, options);

	/// <summary>
	/// Executes the specified operation on a sequence of input elements asynchronously,  leveraging the scaling
	/// capabilities of the provided <see cref="IScalingService"/>.
	/// </summary>
	/// <remarks>This method enables efficient processing of large input sequences by distributing the execution of
	/// the  operation across multiple resources, as managed by the <see cref="IScalingService"/>. The operation is 
	/// executed synchronously for each input element, but the overall processing is asynchronous.</remarks>
	/// <typeparam name="TInput">The type of the input elements in the sequence.</typeparam>
	/// <typeparam name="TReturn">The type of the result produced by the operation for each input element.</typeparam>
	/// <param name="service">The <see cref="IScalingService"/> used to manage and scale the execution of the operation.</param>
	/// <param name="inputs">An asynchronous sequence of input elements to process.</param>
	/// <param name="operation">A synchronous function that defines the operation to perform on each input element.</param>
	/// <param name="options">Optional scaling options that configure the behavior of the scaling service. If null, default options are used.</param>
	/// <returns>An asynchronous sequence of results, where each result corresponds to the output of the operation  applied to an
	/// input element.</returns>
	public static IAsyncEnumerable<TReturn> ExecuteAsync<TInput, TReturn>(this IScalingService service, IAsyncEnumerable<TInput> inputs, Func<TInput, TReturn> operation, TplScalingOptions? options = null)
		=> service.ExecuteAsync(inputs, WrapSync(operation), options);

	/// <summary>
	/// Wraps a synchronous operation in a function that returns a <see cref="Task{TResult}"/>.
	/// </summary>
	/// <typeparam name="TInput">The type of the input parameter for the operation.</typeparam>
	/// <typeparam name="TReturn">The type of the result returned by the operation.</typeparam>
	/// <param name="operation">The synchronous operation to be wrapped. Cannot be <see langword="null"/>.</param>
	/// <returns>A function that takes an input of type <typeparamref name="TInput"/> and returns a <see cref="Task{TResult}"/> 
	/// representing the result of the synchronous operation.</returns>
	public static Func<TInput, Task<TReturn>> WrapSync<TInput, TReturn>(Func<TInput, TReturn> operation)
		=> input => Task.FromResult(operation(input));
}
