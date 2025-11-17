using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;

namespace nc.Models.Tests
{
	public class MockStore<T, TKey>: IStore<T, TKey>
		where T: class
	{
		private readonly ConcurrentDictionary<TKey, T> _store = new();
		
		public async IAsyncEnumerable<T> PostAsync(IAsyncEnumerable<T> items, CancellationToken cancellationToken = default)
		{
			await foreach (var item in items)
			{
				_store.AddOrUpdate(GetId(item), item, (_, _) => item);
				yield return item;
			}
		}

		public async IAsyncEnumerable<T> GetAsync(IAsyncEnumerable<TKey> ids, CancellationToken cancellationToken = default)
		{
			await foreach (var id in ids)
			{
				if (_store.TryGetValue(id, out var item))
					yield return item;
			}
		}

		public IAsyncEnumerable<T> PutAsync(IAsyncEnumerable<T> items, CancellationToken cancellationToken = default)
			=> PostAsync(items, cancellationToken);

		public async Task DeleteAsync(IAsyncEnumerable<TKey> ids, CancellationToken cancellationToken = default)
		{
			await foreach (var id in ids)
				_store.TryRemove(id, out var _);
		}

		public IStoreQuery<T> Query()
		{
			throw new NotImplementedException();
		}

		private TKey GetId(T item)
		{
			var keyProp = typeof(T).GetProperties().FirstOrDefault(p => Attribute.IsDefined(p, typeof(KeyAttribute)));
			var prop = keyProp ?? typeof(T).GetProperty("Id");
			if (prop == null)
				throw new InvalidOperationException($"Type {typeof(T).Name} must have an 'Id' property.");
			var value = prop.GetValue(item);
			if (value == null)
				throw new InvalidOperationException("Id property must not be null.");
			return (TKey)value ?? throw new InvalidOperationException("Id property could not be converted to string.");
		}
	}
	
}
