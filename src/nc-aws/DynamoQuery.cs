using Amazon.DynamoDBv2.DataModel;
using nc.Models;
using System.Linq.Expressions;

namespace nc.Aws;

/// <summary>
/// Represents a query builder for retrieving entities from an Amazon DynamoDB table using LINQ-like expressions.
/// </summary>
/// <remarks>Use this class to construct and execute queries against a DynamoDB table in a composable manner.
/// Methods such as ForPartition, Where, and OrderBy can be chained to build up the query before executing it
/// asynchronously with SearchAsync. This class is intended for use with the AWS DynamoDBContext and supports both
/// partition key queries and full table scans. For large tables, avoid full scans for performance reasons.</remarks>
/// <typeparam name="T">The type of the entities to query. Must be a reference type.</typeparam>
public class DynamoQuery<T> : IStoreQuery<T> where T : class
{
	private readonly IDynamoDBContext _context;
	private readonly string? _partitionKey;
	private Expression<Func<T, bool>>? _predicate;
	private LambdaExpression? _orderBy;

	/// <summary>
	/// Initializes a new instance of the DynamoQuery class using the specified DynamoDB context.
	/// </summary>
	/// <param name="context">The DynamoDB context to use for database operations. Cannot be null.</param>
	public DynamoQuery(IDynamoDBContext context)
	{
		_context = context;
	}

	/// <summary>
	/// Initializes a new instance of the DynamoQuery class with the specified context, partition key, filter predicate,
	/// and ordering expression.
	/// </summary>
	/// <param name="context">The DynamoDB context used to access and query the database. Cannot be null.</param>
	/// <param name="partitionKey">The partition key value to filter the query results. If null, no partition key filter is applied.</param>
	/// <param name="predicate">An optional filter expression used to restrict the query results. If null, all items matching the partition key are
	/// included.</param>
	/// <param name="orderBy">An optional expression specifying the property by which to order the query results. If null, the default ordering
	/// is used.</param>
	private DynamoQuery(IDynamoDBContext context, string? partitionKey, Expression<Func<T, bool>>? predicate, LambdaExpression? orderBy)
	{
		_context = context;
		_partitionKey = partitionKey;
		_predicate = predicate;
		_orderBy = orderBy;
	}

	/// <summary>
	/// Creates a query that targets the specified partition within the data store.
	/// </summary>
	/// <param name="partitionKey">The key identifying the partition to query. Cannot be null or empty.</param>
	/// <returns>An <see cref="IStoreQuery{T}"/> instance scoped to the specified partition.</returns>
	public IStoreQuery<T> ForPartition(string partitionKey)
		=> new DynamoQuery<T>(_context, partitionKey, _predicate, _orderBy);

	/// <summary>
	/// Filters the elements of the query based on a specified predicate.
	/// </summary>
	/// <remarks>The predicate is applied to each element in the query. This method does not execute the query; it
	/// returns a new query object that can be further composed or executed later.</remarks>
	/// <param name="predicate">An expression that defines the conditions each element must satisfy to be included in the result.</param>
	/// <returns>A new query that contains only the elements that satisfy the specified predicate.</returns>
	public IStoreQuery<T> Where(Expression<Func<T, bool>> predicate)
		=> new DynamoQuery<T>(_context, _partitionKey, predicate, _orderBy);

	/// <summary>
	/// Specifies the property by which to order the results of the query in ascending order.
	/// </summary>
	/// <typeparam name="TKey">The type of the key used for ordering the results.</typeparam>
	/// <param name="keySelector">An expression that selects the key to order the results by. Cannot be null.</param>
	/// <returns>An <see cref="IStoreQuery{T}"/> that represents the ordered query.</returns>
	public IStoreQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
		=> new DynamoQuery<T>(_context, _partitionKey, _predicate, keySelector);

	/// <summary>
	/// Asynchronously searches for entities of type T that match the configured criteria.
	/// </summary>
	/// <remarks>The search may perform a full table scan if no partition key is specified, which can impact
	/// performance on large datasets. Server-side ordering is only supported when both a partition key and a sort key are
	/// specified; otherwise, ordering may be performed in memory, which is not recommended for large result
	/// sets.</remarks>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous search operation.</param>
	/// <returns>An asynchronous sequence of entities of type T that satisfy the search criteria.</returns>
	/// <exception cref="NotImplementedException">The method is not implemented.</exception>
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