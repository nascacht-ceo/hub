using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nc.Cloud;
using nc.Models;
using System.Runtime.CompilerServices;

namespace nc.Aws;

/// <summary>
/// Provides an implementation of <see cref="IStore{T}"/> backed by Amazon DynamoDB for managing entities of type <typeparamref
/// name="T"/>.
/// </summary>
/// <remarks>This class supports asynchronous operations for creating, retrieving, updating, and deleting entities
/// in a DynamoDB table. It uses batching for write and read operations to optimize performance and adhere to DynamoDB
/// limits.</remarks>
/// <typeparam name="T">The type of the entities managed by the store. Must be a reference type.</typeparam>
/// <typeparam name="TKey">The type of the entity key managed by the store.</typeparam>
public class DynamoStore<T, TKey> : IStore<T, TKey> where T : class
{
	private readonly DynamoStoreOptions _options;
	private readonly ILogger<DynamoStore<T, TKey>>? _logger;
	private Lazy<Task<IDynamoDBContext>> _context;
	private IAmazonDynamoDB _client;

	/// <summary>
	/// Initializes a new instance of the <see cref="DynamoStore{T, TKey}"/> class, which provides functionality for interacting
	/// with a DynamoDB table to store and retrieve data of type <typeparamref name="T"/>.
	/// </summary>
	/// <remarks>The <see cref="DynamoStore{T, TKey}"/> class is designed to simplify interactions with DynamoDB by
	/// providing a context for storing and retrieving objects of type <typeparamref name="T"/>. The provided options can
	/// be used to customize the behavior of the store, such as timeout settings or context initialization.</remarks>
	/// <param name="client">The <see cref="IAmazonDynamoDB"/> client used to interact with the DynamoDB service. This parameter cannot be null.</param>
	/// <param name="options">Optional configuration settings for the <see cref="DynamoStore{T, TKey}"/> instance. If not provided, default options
	/// will be used.</param>
	/// <param name="logger">An optional logger instance of type <see cref="ILogger{TCategoryName}"/> for logging diagnostic messages.</param>
	public DynamoStore(IAmazonDynamoDB client, IOptions<DynamoStoreOptions<T>>? options = null, ILogger<DynamoStore<T, TKey>>? logger = null)
	{
		_client = client;
		_options = options?.Value ?? new DynamoStoreOptions();
		_logger = logger;
		_context = new Lazy<Task<IDynamoDBContext>>(async () =>
		{
			var cts = new CancellationTokenSource(_options.Timeout);
			return await _options.GetContextAsync<T>(_client, _logger, cts.Token);
		});

	}

	/// <summary>
	/// Writes a sequence of items to a DynamoDB table in batches and yields each successfully written item.
	/// </summary>
	/// <remarks>This method processes the input sequence in batches, with each batch containing up to 25 items,
	/// which is the maximum batch size supported by DynamoDB's BatchWrite operation. If the batch size is configured to a
	/// value less than or equal to 0, or greater than 25, the default batch size of 25 is used.  The method ensures that
	/// each item is yielded only after it has been successfully written to the table. If the operation is canceled via the
	/// <paramref name="cancellationToken"/>, the method stops processing and no further items are written or yielded. 
	/// This method is designed to handle large sequences efficiently by processing them in batches, which minimizes the
	/// number of network calls to DynamoDB.</remarks>
	/// <param name="items">An asynchronous sequence of items to be written to the DynamoDB table.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>An asynchronous stream of items that were successfully written to the DynamoDB table.</returns>
	public async IAsyncEnumerable<T> PostAsync(IAsyncEnumerable<T> items, [EnumeratorCancellation]CancellationToken cancellationToken = default)
	{
		var context = await _context.Value;
		int batchSize = _options.BatchSizeWrite > 0 && _options.BatchSizeWrite <= 25 ? _options.BatchSizeWrite : 25; // DynamoDB BatchWrite max is 25
		var batchWrite = context.CreateBatchWrite<T>(_options.BatchWriteConfig);
		var count = 0;
		var written = new List<T>(batchSize);
		await foreach (var item in items.WithCancellation(cancellationToken))
		{
			batchWrite.AddPutItem(item);
			written.Add(item);
			if (++count == batchSize)
			{
				_logger?.LogTrace("Writing batch of {Count} items to DynamoDB table {DynamoDbTable}.", count, _options.TableName);
				await batchWrite.ExecuteAsync(cancellationToken);

				foreach (var entity in written)
					yield return entity;
				batchWrite = context.CreateBatchWrite<T>(_options.BatchWriteConfig);
				count = 0;
				written.Clear();
			}
		}

		if (count > 0)
		{
			_logger?.LogTrace("Writing final batch of {Count} items to DynamoDB table {DynamoDbTable}.", count, _options.TableName);
			await batchWrite.ExecuteAsync(cancellationToken);
			foreach (var entity in written)
				yield return entity;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="items"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public IAsyncEnumerable<T> PutAsync(IAsyncEnumerable<T> items, CancellationToken cancellationToken = default)
		=> PostAsync(items, cancellationToken);

	/// <summary>
	/// Retrieves items from a data source asynchronously in batches based on the provided identifiers.
	/// </summary>
	/// <remarks>This method processes the identifiers in batches to optimize retrieval performance. The batch size
	/// is determined by the configuration and defaults to 100 if not explicitly set or if the configured value is outside
	/// the valid range (1 to 100).  If an identifier does not correspond to an item in the data source, it is skipped, and
	/// no item is yielded for that identifier. The method ensures that all valid items are retrieved and yielded in the
	/// order they are processed.  The caller can cancel the operation at any time by passing a <see
	/// cref="CancellationToken"/>. If cancellation is requested, the method stops processing and no further items are
	/// yielded.</remarks>
	/// <param name="ids">An asynchronous sequence of identifiers representing the items to retrieve. Each identifier corresponds to an item
	/// in the data source.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>An asynchronous sequence of items of type <typeparamref name="T"/> that were successfully retrieved. Items are
	/// yielded as they are retrieved, and the sequence ends when all identifiers have been processed.</returns>
	public async IAsyncEnumerable<T> GetAsync(IAsyncEnumerable<TKey> ids, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var context = await _context.Value;
		int batchSize = _options.BatchSizeGet > 0 && _options.BatchSizeGet <= 100 ? _options.BatchSizeGet : 100;
		var batchGet = context.CreateBatchGet<T>(_options.BatchGetConfig);

		await foreach (var id in ids)
		{
			batchGet.AddKey(id);
			if (batchGet.TotalKeys == batchSize)
			{
				_logger?.LogTrace("Getting batch of {Count} items from DynamoDB table {DynamoDbTable}.", batchGet.TotalKeys, _options.TableName);
				await batchGet.ExecuteAsync(cancellationToken);
				foreach (var item in batchGet.Results)
					if (item != null)
						yield return item;
				batchGet = context.CreateBatchGet<T>();
			}
		}
		if (batchGet.TotalKeys > 0)
		{
			_logger?.LogTrace("Getting final batch of {Count} items from DynamoDB table {DynamoDbTable}.", batchGet.TotalKeys, _options.TableName);
			await batchGet.ExecuteAsync(cancellationToken);
			foreach (var item in batchGet.Results)
				if (item != null)
					yield return item;
		}
	}

	/// <summary>
	/// Deletes a collection of items from the DynamoDB table asynchronously.
	/// </summary>
	/// <remarks>The method processes the deletions in batches to optimize performance. The batch size is determined
	/// by the  <c>BatchSizeWrite</c> option, which defaults to 25 if not configured or set to an invalid value.  If the
	/// number of items in the sequence is not a multiple of the batch size, a final batch will be processed  for the
	/// remaining items.</remarks>
	/// <param name="ids">An asynchronous sequence of item identifiers to delete. Each identifier corresponds to an item in the DynamoDB
	/// table.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task that represents the asynchronous delete operation.</returns>
	public async Task DeleteAsync(IAsyncEnumerable<TKey> ids, CancellationToken cancellationToken = default)
	{
		var context = await _context.Value;
		int batchSize = _options.BatchSizeWrite > 0 && _options.BatchSizeWrite <= 25 ? _options.BatchSizeWrite : 25; 
		var batchWrite = context.CreateBatchWrite<T>(_options.BatchWriteConfig);
		var count = 0;
		await foreach (var id in ids.WithCancellation(cancellationToken))
		{
			batchWrite.AddDeleteKey(id);

			if (++count == batchSize)
			{
				_logger?.LogTrace("Deleting batch of {Count} items from DynamoDB table {DynamoDbTable}.", count, _options.TableName);
				await batchWrite.ExecuteAsync(cancellationToken);
				batchWrite = context.CreateBatchWrite<T>(_options.BatchWriteConfig);
				count = 0;
			}
		}

		if (count > 0)
		{
			_logger?.LogTrace("Deleting final batch of {Count} items from DynamoDB table {DynamoDbTable}.", count, _options.TableName);
			await batchWrite.ExecuteAsync(cancellationToken);

		}
	}

	public IStoreQuery<T> Query()
	{
		throw new NotImplementedException("Querying is not implemented in this example.");
	}
}