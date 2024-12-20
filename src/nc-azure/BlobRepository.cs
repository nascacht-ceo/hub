using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

public class BlobRepository : IRepository<ICloudFile>
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _container;
    private readonly ILogger? _logger;
    private DataflowBlockOptions _blockOptions = new ExecutionDataflowBlockOptions
    {
        MaxDegreeOfParallelism = 4
    };

    public BlobRepository(BlobServiceClient blobServiceClient, string container, ILogger? logger = null)
    {
        _blobServiceClient = blobServiceClient;
        _container = container;
        _logger = logger;

    }

    public async Task<ICloudFile> DeleteAsync(ICloudFile file, CancellationToken cancellationToken = default)
    {
        if (_container == null)
            throw new ArgumentNullException(nameof(_container), "An Azure blob storgae container name must be set before calling this method.");
        var containerClient = _blobServiceClient.GetBlobContainerClient(_container);
        var batchClient = _blobServiceClient.GetBlobBatchClient();

        var deleteUris = new List<Uri>();

        if (string.IsNullOrEmpty(file.Path))
            throw new ArgumentNullException(nameof(file.Path), "File path must be set before calling this method.");
        var cleanPath = file.Path.ToCloudFilePath();
        // Check if path represents a folder
        if (cleanPath.EndsWith("/"))
        {
            // Recursively delete blobs within the folder
            var blobs = containerClient.GetBlobsAsync(prefix: cleanPath);
            await foreach (var blob in blobs)
            {
                var blobUri = containerClient.GetBlobClient(blob.Name).Uri;
                deleteUris.Add(blobUri);
            }
        }
        else
        {
            // Add single file to the list
            var blobUri = containerClient.GetBlobClient(cleanPath).Uri;
            deleteUris.Add(blobUri);
        }

        // Perform batch delete
        await batchClient.DeleteBlobsAsync(deleteUris);
        return file;
    }

    /// <summary>
    /// Save files to Azure Blob Storage.
    /// </summary>
    /// <param name="values">Enumeration of </param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<ICloudFile> SaveAsync(ICloudFile file, CancellationToken cancellationToken = default)
    {
        if (file == null) throw new ArgumentNullException(nameof(file));
        if (string.IsNullOrEmpty(file.Path))
            throw new ArgumentNullException(nameof(file.Path), "File path must be set before calling this method.");

        try
        {
            var directoryPath = file.Path.ToCloudFilePath();
            var containerClient = _blobServiceClient.GetBlobContainerClient(_container);
            var blobClient = containerClient.GetBlobClient(directoryPath);
            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = file.ContentType
            };

            using var stream = await file.CreateReadStreamAsync();

            var info = await blobClient.UploadAsync(stream, blobHttpHeaders, cancellationToken: cancellationToken);

            if (file.Metadata != null && file.Metadata.Count > 0)
                await blobClient.SetMetadataAsync(file.Metadata, cancellationToken: cancellationToken);

            return new BlobFile(blobClient, info.Value);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to upload file to Azure Blob Storage.");
            throw;
        }
    }


    public IAsyncEnumerable<ICloudFile> SearchAsync(IQuery<ICloudFile> query, CancellationToken cancellationToken = default)
    {
        if (_container == null)
            throw new ArgumentNullException(nameof(_container), "An Azure blob storgae container name must be set before calling this method.");
        var fileQuery = query as ICloudFileQuery;
        if (fileQuery == null)
            throw new ArgumentOutOfRangeException(nameof(query), "BlobRepository expects a CloudFileQuery parameters.");
        var directoryPath = fileQuery.Path.ToCloudFilePath();

        var containerClient = _blobServiceClient.GetBlobContainerClient(_container);
        return containerClient
            .GetBlobsByHierarchyAsync(prefix: directoryPath, delimiter: fileQuery.FolderDelimiter)
            .Select(blob =>
            {
                var client = (blob.IsPrefix)
                    ? containerClient.GetBlobClient(blob.Prefix)
                    : containerClient.GetBlobClient(blob.Blob.Name);
                return new BlobFile(client, blob);
            });
    }
}
