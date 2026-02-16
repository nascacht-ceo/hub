using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace nc.Models;

/// <summary>
/// An in-memory implementation of <see cref="IStore{T, TKey}"/> backed by a <see cref="ConcurrentDictionary{TKey, TValue}"/>.
/// </summary>
/// <remarks>
/// This store is useful for testing, caching, or scenarios where persistence is not required.
/// The key is extracted from entities using either a property decorated with <see cref="KeyAttribute"/>
/// or a property named "Id".
/// </remarks>
/// <typeparam name="T">The type of the entities managed by the store. Must be a reference type.</typeparam>
/// <typeparam name="TKey">The type of the key used to identify entities.</typeparam>
public class MemoryStore<T, TKey> : IStore<T, TKey>
	where T : class
	where TKey : notnull
{
	private readonly ConcurrentDictionary<TKey, T> _store = new();

	/// <inheritdoc/>
	public async IAsyncEnumerable<T> PostAsync(IAsyncEnumerable<T> items, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		await foreach (var item in items.WithCancellation(cancellationToken))
		{
			var key = GetKey(item);
			_store.AddOrUpdate(key, item, (_, _) => item);
			yield return item;
		}
	}

	/// <inheritdoc/>
	public async IAsyncEnumerable<T> GetAsync(IAsyncEnumerable<TKey> ids, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		await foreach (var id in ids.WithCancellation(cancellationToken))
		{
			if (_store.TryGetValue(id, out var item))
				yield return item;
		}
	}

	/// <inheritdoc/>
	public IAsyncEnumerable<T> PutAsync(IAsyncEnumerable<T> items, CancellationToken cancellationToken = default)
		=> PostAsync(items, cancellationToken);

	/// <inheritdoc/>
	public async Task DeleteAsync(IAsyncEnumerable<TKey> ids, CancellationToken cancellationToken = default)
	{
		await foreach (var id in ids.WithCancellation(cancellationToken))
			_store.TryRemove(id, out _);
	}

	/// <inheritdoc/>
	public IStoreQuery<T> Query() => new MemoryStoreQuery(_store.Values);

	/// <summary>
	/// Extracts the key from an entity using the property decorated with <see cref="KeyAttribute"/>
	/// or a property named "Id".
	/// </summary>
	private static TKey GetKey(T item)
	{
		var keyProp = typeof(T).GetProperties()
			.FirstOrDefault(p => Attribute.IsDefined(p, typeof(KeyAttribute)))
			?? typeof(T).GetProperty("Id");

		if (keyProp == null)
			throw new InvalidOperationException($"Type {typeof(T).Name} must have a property decorated with [Key] or named 'Id'.");

		var value = keyProp.GetValue(item)
			?? throw new InvalidOperationException("Key property must not be null.");

		return (TKey)value;
	}

	private class MemoryStoreQuery : IStoreQuery<T>
	{
		private IEnumerable<T> _items;

		public MemoryStoreQuery(IEnumerable<T> items)
		{
			_items = items;
		}

		public IStoreQuery<T> ForPartition(string partitionKey) => this;

		public IStoreQuery<T> Where(Expression<Func<T, bool>> predicate)
		{
			_items = _items.Where(predicate.Compile());
			return this;
		}

		public IStoreQuery<T> OrderBy<TOrderKey>(Expression<Func<T, TOrderKey>> keySelector)
		{
			_items = _items.OrderBy(keySelector.Compile());
			return this;
		}

		public async IAsyncEnumerable<T> SearchAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			foreach (var item in _items)
			{
				cancellationToken.ThrowIfCancellationRequested();
				yield return item;
			}
			await Task.CompletedTask;
		}
	}
}

/// <summary>
/// An in-memory implementation of <see cref="IStore{T}"/> with string keys.
/// </summary>
/// <typeparam name="T">The type of the entities managed by the store. Must be a reference type.</typeparam>
public class MemoryStore<T> : MemoryStore<T, string>, IStore<T>
	where T : class
{
}
