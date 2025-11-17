using Microsoft.EntityFrameworkCore;
using nc.Models;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace nc.Data;

public class EntityFrameworkStore<T, TKey> : DbContext, IStore<T, TKey> where T : class
{
	public DbSet<T> Entities { get; set; }
	private readonly PropertyInfo _keyProperty;

	public EntityFrameworkStore(DbContextOptions options)
		: base(options)
	{
		_keyProperty = GetKeyProperty();
	}

	private PropertyInfo GetKeyProperty()
	{
		var type = typeof(T);
		var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

		// Look for [Key] attribute
		var keyProp = props.FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
		if (keyProp != null) return keyProp;

		// Fallback: property named "Id"
		keyProp = props.FirstOrDefault(p => string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase));
		if (keyProp != null) return keyProp;

		// Fallback: first string property
		keyProp = props.FirstOrDefault(p => p.PropertyType == typeof(string));
		if (keyProp != null) return keyProp;

		throw new InvalidOperationException($"No suitable key property found for type {type.Name}. Mark a property with [Key] or name it 'Id'.");
	}

	private TKey GetKey(T entity)
	{
		return (TKey)_keyProperty.GetValue(entity)!;
	}

	public async IAsyncEnumerable<T> PostAsync(IAsyncEnumerable<T> items, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		await foreach (var item in items.WithCancellation(cancellationToken))
		{
			await Entities.AddAsync(item, cancellationToken);
			yield return item;
		}
		await SaveChangesAsync(cancellationToken);
	}

	public async IAsyncEnumerable<T> GetAsync(IAsyncEnumerable<TKey> ids, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var idList = new List<TKey>();
		await foreach (var id in ids.WithCancellation(cancellationToken))
			idList.Add(id);

		foreach (var entity in Entities)
		{
			if (idList.Contains(GetKey(entity)))
				yield return entity;
		}
	}

	public async IAsyncEnumerable<T> PutAsync(IAsyncEnumerable<T> items, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		await foreach (var item in items.WithCancellation(cancellationToken))
		{
			Entities.Update(item);
			yield return item;
		}
		await SaveChangesAsync(cancellationToken);
	}

	public async Task DeleteAsync(IAsyncEnumerable<TKey> ids, CancellationToken cancellationToken = default)
	{
		var idList = new List<TKey>();
		await foreach (var id in ids.WithCancellation(cancellationToken))
			idList.Add(id);

		var toRemove = Entities.Where(e => idList.Contains(GetKey(e))).ToList();
		Entities.RemoveRange(toRemove);
		await SaveChangesAsync(cancellationToken);
	}

	public IStoreQuery<T> Query()
	{
		return new EfStoreQuery<T>(Entities.AsQueryable());
	}

	private class EfStoreQuery<TQuery> : IStoreQuery<TQuery> where TQuery : class
	{
		private IQueryable<TQuery> _query;

		public EfStoreQuery(IQueryable<TQuery> query)
		{
			_query = query;
		}

		public IStoreQuery<TQuery> ForPartition(string partitionKey) => this;

		public IStoreQuery<TQuery> Where(Expression<Func<TQuery, bool>> predicate)
		{
			_query = _query.Where(predicate);
			return this;
		}

		public IStoreQuery<TQuery> OrderBy<TKey>(Expression<Func<TQuery, TKey>> keySelector)
		{
			_query = _query.OrderBy(keySelector);
			return this;
		}

		public async IAsyncEnumerable<TQuery> SearchAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			foreach (var entity in _query)
			{
				cancellationToken.ThrowIfCancellationRequested();
				yield return entity;
			}
			await Task.Yield();
		}
	}
}