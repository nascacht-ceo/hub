using System.Linq.Expressions;

namespace nc.Models;

/// <summary>
/// Defines a generic interface for a data store that supports basic CRUD operations  and fluent query building for
/// entities of type <typeparamref name="T"/>.
/// </summary>
/// <remarks>This interface provides asynchronous methods for creating, reading, updating,  and deleting entities,
/// as well as a method to initiate a fluent query builder  for advanced querying scenarios.</remarks>
/// <typeparam name="T">The type of the entities managed by the store. Must be a reference type.</typeparam>
public interface IStore<T, TKey> where T: class
{
	/// <summary>
	/// Sends a collection of items to the server asynchronously and returns the processed items.
	/// </summary>
	/// <remarks>This method performs an asynchronous operation to send the provided items to the server.  The
	/// returned collection contains the items after processing by the server.  Ensure that the input collection is not
	/// null before calling this method.</remarks>
	/// <param name="items">The collection of items to be sent. Cannot be null.</param>
	/// <returns>An <see cref="IEnumerable{T}"/> containing the processed items returned by the server.</returns>
	IAsyncEnumerable<T> PostAsync(IAsyncEnumerable<T> items, CancellationToken cancellationToken = default);

	/// <summary>
	/// Asynchronously retrieves a collection of items corresponding to the specified identifiers.
	/// </summary>
	/// <remarks>The method does not guarantee the order of the returned items relative to the input identifiers. 
	/// If an identifier does not correspond to an existing item, it will be ignored.</remarks>
	/// <param name="ids">A collection of unique identifiers representing the items to retrieve. Each identifier must be a non-null,
	/// non-empty string.</param>
	/// <returns>An enumerable collection of items of type <typeparamref name="T"/> that match the specified identifiers.  The
	/// collection will be empty if no matching items are found.</returns>
	IAsyncEnumerable<T> GetAsync(IAsyncEnumerable<TKey> ids, CancellationToken cancellationToken = default);

	// Update (PUT)
	IAsyncEnumerable<T> PutAsync(IAsyncEnumerable<T> items, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes the resources identified by the specified collection of IDs asynchronously.
	/// </summary>
	/// <remarks>The method processes the provided IDs asynchronously and deletes the corresponding resources. If
	/// the sequence of IDs is empty, no action is performed. Ensure that the caller has the necessary permissions to
	/// delete the specified resources.</remarks>
	/// <param name="ids">An asynchronous sequence of strings representing the IDs of the resources to delete. Each ID must be a non-null,
	/// non-empty string.</param>
	/// <returns>A task that represents the asynchronous delete operation.</returns>
	Task DeleteAsync(IAsyncEnumerable<TKey> ids, CancellationToken cancellationToken = default);

	/// <summary>
	/// Starts a fluent query builder chain.
	/// </summary>
	/// <returns>An instance of the constrained IStoreQuery<T> implementation.</returns>
	IStoreQuery<T> Query();

	/// <summary>
	/// Sends a collection of items asynchronously and returns an asynchronous stream of results.
	/// </summary>
	/// <param name="items">The collection of items to be sent. Cannot be null.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>An asynchronous stream of results corresponding to the sent items. The stream may be empty if no results are
	/// produced.</returns>
	public virtual IAsyncEnumerable<T> PostAsync(IEnumerable<T> items, CancellationToken cancellationToken = default) 
		=> PostAsync(items.ToAsyncEnumerable(), cancellationToken);

	/// <summary>
	/// Asynchronously processes and stores a collection of items.
	/// </summary>
	/// <remarks>This method accepts a collection of items and processes them asynchronously. The caller can use the
	/// returned asynchronous stream to iterate over the processed items as they become available. If the operation is
	/// canceled via the <paramref name="cancellationToken"/>, the returned stream will terminate early.</remarks>
	/// <param name="items">The collection of items to be processed and stored.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>An asynchronous stream of items of type <typeparamref name="T"/> representing the processed results.</returns>
	public virtual IAsyncEnumerable<T> PutAsync(IEnumerable<T> items, CancellationToken cancellationToken = default) 
		=> PutAsync(items.ToAsyncEnumerable(), cancellationToken);

	/// <summary>
	/// Retrieves a collection of entities asynchronously based on the specified identifiers.
	/// </summary>
	/// <remarks>This method supports asynchronous enumeration, allowing the caller to process the results as they
	/// are retrieved.</remarks>
	/// <param name="ids">A collection of identifiers representing the entities to retrieve. Each identifier corresponds to an entity.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>An asynchronous stream of entities corresponding to the specified identifiers. The stream may be empty if no
	/// entities are found.</returns>
	public virtual IAsyncEnumerable<T> GetAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
		=> GetAsync(ids.ToAsyncEnumerable(), cancellationToken);

	/// <summary>
	/// Deletes the entities corresponding to the specified collection of identifiers asynchronously.
	/// </summary>
	/// <remarks>This method deletes all entities associated with the provided identifiers. If the collection of
	/// identifiers is empty, no operation is performed. The operation is performed asynchronously and respects the
	/// provided cancellation token.</remarks>
	/// <param name="ids">A collection of identifiers representing the entities to delete.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
	/// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
	public virtual Task DeleteAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default) 
		=> DeleteAsync(ids.ToAsyncEnumerable(), cancellationToken);

	/// <summary>
	/// Posts the specified item asynchronously and returns the result.
	/// </summary>
	/// <remarks>This method posts the provided item and processes it asynchronously. The operation may involve
	/// additional asynchronous steps, and the caller should await the returned task to ensure completion.</remarks>
	/// <param name="item">The item to be posted.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
	/// <returns>A <see cref="ValueTask{T}"/> representing the asynchronous operation. The task result contains the processed item
	/// of type <typeparamref name="T"/>.</returns>
	public virtual ValueTask<T> PostAsync(T item, CancellationToken cancellationToken = default) 
		=> PostAsync(OneAsync(item), cancellationToken).FirstAsync(cancellationToken);

	/// <summary>
	/// Asynchronously adds or updates the specified item in the collection.
	/// </summary>
	/// <remarks>This method ensures that the specified item is added to the collection or updated if it already
	/// exists. The operation is performed asynchronously and supports cancellation through the provided <paramref
	/// name="cancellationToken"/>.</remarks>
	/// <param name="item">The item to add or update in the collection.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>A <see cref="ValueTask{T}"/> that represents the asynchronous operation. The task result contains the item that was
	/// added or updated.</returns>
	public virtual ValueTask<T> PutAsync(T item, CancellationToken cancellationToken = default)
		=> PutAsync(OneAsync(item), cancellationToken).FirstAsync(cancellationToken);

	/// <summary>
	/// Asynchronously retrieves an entity by its identifier.
	/// </summary>
	/// <remarks>This method returns the first entity that matches the specified identifier. If no entity is found,
	/// the result is <see langword="null"/>.</remarks>
	/// <param name="id">The unique identifier of the entity to retrieve.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
	/// <returns>A <see cref="ValueTask{T}"/> representing the asynchronous operation. The task result contains the entity if found;
	/// otherwise, <see langword="null"/>.</returns>
	public virtual ValueTask<T?> GetAsync(TKey id, CancellationToken cancellationToken = default)
		=> GetAsync(OneAsync(id), cancellationToken).FirstOrDefaultAsync(cancellationToken);

	/// <summary>
	/// Deletes an entity with the specified identifier asynchronously.
	/// </summary>
	/// <remarks>If the entity with the specified identifier does not exist, the operation may complete without
	/// making any changes.</remarks>
	/// <param name="id">The unique identifier of the entity to delete.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	public virtual Task DeleteAsync(TKey id, CancellationToken cancellationToken = default) 
		=> DeleteAsync(OneAsync(id), cancellationToken);

	/// <summary>
	/// Asynchronously returns a single item in an <see cref="IAsyncEnumerable{T}"/> sequence.
	/// </summary>
	/// <remarks>This method ensures asynchronous behavior by yielding the item asynchronously. It is useful for
	/// scenarios where an asynchronous sequence with a single item is required.</remarks>
	/// <typeparam name="TItem">The type of the item to be returned in the sequence.</typeparam>
	/// <param name="item">The item to be included in the asynchronous sequence.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> containing the specified item.</returns>
	private static async IAsyncEnumerable<TItem> OneAsync<TItem>(TItem item)
	{
		yield return item;
		await Task.Yield();
	}
}

public interface IStoreQuery<T> where T: class
{
	IStoreQuery<T> ForPartition(string partitionKey);
	IStoreQuery<T> Where(Expression<Func<T, bool>> predicate);
	IStoreQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector);

	IAsyncEnumerable<T> SearchAsync(CancellationToken cancellationToken = default);
}

public interface IStore<T>: IStore<T, string> where T: class
{ }
