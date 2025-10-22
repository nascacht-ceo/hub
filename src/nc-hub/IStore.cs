using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Hub;

/// <summary>
/// Defines a generic interface for a data store that supports asynchronous read, write, and delete operations.
/// </summary>
/// <typeparam name="T">The type of items managed by the store. 
/// This type represents both the data to be stored and the keys used for deletion.</typeparam>
/// <typeparam name="TKey">The type of the key used to identify items in the store.</typeparam>
public interface IStore<T, TKey>
{
	public IAsyncEnumerable<T> ReadAsync();

	Task WriteAsync(IEnumerable<T> items);
	Task DeleteAsync(IEnumerable<TKey> keys);
}
