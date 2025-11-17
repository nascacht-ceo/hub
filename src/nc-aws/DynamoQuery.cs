using Amazon.DynamoDBv2.DataModel;
using nc.Models;
using System.Linq.Expressions;

namespace nc.Aws;

public class DynamoQuery<T> : IStoreQuery<T> where T : class
{
	private readonly IDynamoDBContext _context;
	private readonly string? _partitionKey;
	private Expression<Func<T, bool>>? _predicate;
	private LambdaExpression? _orderBy;

	public DynamoQuery(IDynamoDBContext context)
	{
		_context = context;
	}

	private DynamoQuery(IDynamoDBContext context, string? partitionKey, Expression<Func<T, bool>>? predicate, LambdaExpression? orderBy)
	{
		_context = context;
		_partitionKey = partitionKey;
		_predicate = predicate;
		_orderBy = orderBy;
	}

	public IStoreQuery<T> ForPartition(string partitionKey)
		=> new DynamoQuery<T>(_context, partitionKey, _predicate, _orderBy);

	public IStoreQuery<T> Where(Expression<Func<T, bool>> predicate)
		=> new DynamoQuery<T>(_context, _partitionKey, predicate, _orderBy);

	public IStoreQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
		=> new DynamoQuery<T>(_context, _partitionKey, _predicate, keySelector);

	public IAsyncEnumerable<T> SearchAsync(CancellationToken cancellationToken = default)
	{
		//  IQueryable<T> q;
		throw new NotImplementedException();
		// Only support server-side ordering if ordering by sort key and partition key is specified
		//bool canServerOrder = _partitionKey != null && _orderBy != null && IsSortKey(_orderBy);

		//if (_partitionKey != null)
		//{
		//	var config = canServerOrder
		//		? new QueryConfig()
		//		{
					
		//			QueryFilter = null,
		//			BackwardQuery = IsDescending(_orderBy)
		//		}
		//		: null;

		//	var search = _context.QueryAsync<T>(_partitionKey, config);

		//	await foreach (var item in search.ToAsyncEnumerable().WithCancellation(cancellationToken))
		//	{
		//		if (_predicate == null || _predicate.Compile().Invoke(item))
		//			yield return item;
		//	}
		//}
		//else
		//{
		//	// Full table scan (not recommended for large tables)
		//	var search = _context.ScanAsync<T>(null);

		//	await foreach (var item in search.ToAsyncEnumerable().WithCancellation(cancellationToken))
		//	{
		//		if (_predicate == null || _predicate.Compile().Invoke(item))
		//			yield return item;
		//	}
		//}

		// If ordering is by a non-sort key, you must buffer and order in-memory (not scalable for large sets)
		// You can add a warning or throw if _orderBy != null && !canServerOrder
	}
}