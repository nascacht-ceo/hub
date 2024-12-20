using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implement <see cref="ICloudFileService"/> for Azure Blob Storage.
/// </summary>
public class CloudFileService : ICloudFileService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger? _logger;
    private readonly CloudFileServiceOptions? _options;

    /// <summary>
    /// Constructor.
    /// </summary>
    public CloudFileService(CloudFileServiceOptions options, ILogger? logger = null)
    {
        _options = options;
        if (options.ConnectionString != null)
            _blobServiceClient = new BlobServiceClient(options.ConnectionString, options.BlobClientOptions);
        else 
            throw new ArgumentOutOfRangeException(nameof(options), "Either ConnectionString or StorgaeAccount and AccessKey must be set.");
        _logger = logger;
    }

    public CloudFileService(BlobServiceClient client, ILogger? logger = null)
    {
        _blobServiceClient = client;
        _logger = logger;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task<ICloudFileProvider> CreateAsync(string name, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(name);
        await containerClient.CreateIfNotExistsAsync(metadata: metadata);
        return new CloudFileProvider(_blobServiceClient, name, _logger);
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public IAsyncEnumerable<ICloudFileProvider> SearchAsync(string name, CancellationToken cancellationToken = default)
    {
        var containers = _blobServiceClient.GetBlobContainersAsync();
        return containers
            .Where(container => container.Name.IsWildcardMatch(name))
            .Select(container => new CloudFileProvider(_blobServiceClient, container.Name));
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task DeleteAsync(string containerName = null, CancellationToken cancellationToken = default)
    {
        if (containerName == null)
            throw new ArgumentNullException(nameof(containerName), "You must specify a container name to delete.");

        try
        {
            // Get a reference to the container
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // Delete the container if it exists
            await blobContainerClient.DeleteIfExistsAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error trying to delete an Azure Blob Storage container.");
            throw;
        }
    }

}
