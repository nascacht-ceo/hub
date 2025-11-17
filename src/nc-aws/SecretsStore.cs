using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nc.Cloud;
using nc.Models;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace nc.Aws;

public class SecretsStore<T, TKey> : IStore<T, TKey> where T : class
{
    private readonly IAmazonSecretsManager _client;
	private readonly SecretsStoreOptions<T, TKey> _options;
	private readonly ILogger<SecretsStore<T, TKey>>? _logger;
    //private readonly JsonSerializerOptions _jsonOptions;
    //private readonly int _maxDegreeOfParallelism = 6; // Tune as needed

    public SecretsStore(
        IAmazonSecretsManager client,
        ILogger<SecretsStore<T, TKey>>? logger = null,
        IOptions<SecretsStoreOptions<T, TKey>>? options = null)
    {
        _client = client;
        _options = options?.Value ?? new SecretsStoreOptions<T, TKey>();
        _logger = logger;
        //_jsonOptions = jsonOptions ?? new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        //if (maxDegreeOfParallelism.HasValue)
        //    _maxDegreeOfParallelism = maxDegreeOfParallelism.Value;
    }

    public async IAsyncEnumerable<T> PostAsync(IAsyncEnumerable<T> items, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var itemList = new List<T>();
        await foreach (var item in items.WithCancellation(cancellationToken))
            itemList.Add(item);

        var results = new List<T>(itemList.Count);
        await Parallel.ForEachAsync(itemList, _options.GetParallelOptions(cancellationToken),
            async (item, ct) =>
            {
                var id = GetId(item);
                var json = JsonSerializer.Serialize(item, _options.JsonOptions);

                try
                {
                    await _client.CreateSecretAsync(new CreateSecretRequest
                    {
                        Name = id,
                        SecretString = json
                    }, ct);

                    _logger?.LogTrace("Created secret {SecretName}", id);
                }
                catch (ResourceExistsException)
                {
                    await _client.PutSecretValueAsync(new PutSecretValueRequest
                    {
                        SecretId = id,
                        SecretString = json
                    }, ct);

                    _logger?.LogTrace("Updated secret {SecretName}", id);
                }

                lock (results)
                {
                    results.Add(item);
                }
            });

        foreach (var item in results)
            yield return item;
    }

    public IAsyncEnumerable<T> PutAsync(IAsyncEnumerable<T> items, CancellationToken cancellationToken = default)
        => PostAsync(items, cancellationToken);

    public async IAsyncEnumerable<T> GetAsync(IAsyncEnumerable<TKey> ids, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var idList = new List<TKey>();
        await foreach (var id in ids.WithCancellation(cancellationToken))
            idList.Add(id);

        var results = new List<T>();
        await Parallel.ForEachAsync(idList, _options.GetParallelOptions(cancellationToken),
            async (id, ct) =>
            {
                var idString = KeyToString(id);
                try
                {
                    var response = await _client.GetSecretValueAsync(new GetSecretValueRequest
                    {
                        SecretId = idString
                    }, ct);

                    if (!string.IsNullOrEmpty(response.SecretString))
                    {
                        var entity = JsonSerializer.Deserialize<T>(response.SecretString, _options.JsonOptions);
                        if (entity != null)
                        {
                            lock (results)
                            {
                                results.Add(entity);
                            }
                        }
                    }
                }
                catch (ResourceNotFoundException)
                {
                    _logger?.LogDebug("Secret {SecretName} not found", idString);
                }
            });

        foreach (var entity in results)
            yield return entity;
    }

    public async Task DeleteAsync(IAsyncEnumerable<TKey> ids, CancellationToken cancellationToken = default)
    {
        var idList = new List<TKey>();
        await foreach (var id in ids.WithCancellation(cancellationToken))
            idList.Add(id);

        await Parallel.ForEachAsync(idList, _options.GetParallelOptions(cancellationToken),
            async (id, ct) =>
            {
                var idString = KeyToString(id);
                try
                {
                    await _client.DeleteSecretAsync(new DeleteSecretRequest
                    {
                        SecretId = idString,
                        ForceDeleteWithoutRecovery = true
                    }, ct);

                    _logger?.LogTrace("Deleted secret {SecretName}", idString);
                }
                catch (ResourceNotFoundException)
                {
                    _logger?.LogDebug("Secret {SecretName} not found for deletion", idString);
                }
            });
    }

    public IStoreQuery<T> Query()
    {
        throw new NotImplementedException("Querying is not supported for SecretsStore.");
    }

    // Helper: Extract the ID from the entity (assumes a property named "Id" of type TKey or convertible to string)
    private static string GetId(T item)
    {
        var prop = typeof(T).GetProperty("Id");
        if (prop == null)
            throw new InvalidOperationException($"Type {typeof(T).Name} must have an 'Id' property.");
        var value = prop.GetValue(item);
        if (value == null)
            throw new InvalidOperationException("Id property must not be null.");
        return value.ToString() ?? throw new InvalidOperationException("Id property could not be converted to string.");
    }

    // Helper: Convert TKey to string for AWS SecretId
    private static string KeyToString(TKey key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        return key.ToString() ?? throw new InvalidOperationException("Key could not be converted to string.");
    }
}