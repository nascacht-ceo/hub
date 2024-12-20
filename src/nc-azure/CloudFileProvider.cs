using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;

public class CloudFileProvider : ICloudFileProvider
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _container;
    private readonly ILogger? _logger;

    public string Name => _container;

    public CloudFileProvider(BlobServiceClient blobServiceClient, string container, ILogger? logger = null)
    {
        _blobServiceClient = blobServiceClient;
        _container = container;
        _logger = logger;
    }

    public async Task<ICloudFileInfo> GetFileInfoAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (_container == null)
            throw new ArgumentNullException(nameof(_container), "An Azure blob storgae container name must be set before calling this method.");
        filePath = filePath.ToCloudFilePath();
        var containerClient = _blobServiceClient.GetBlobContainerClient(_container);

        var blobClient = containerClient.GetBlobClient(filePath);
        try
        {
            var properties = await blobClient.GetPropertiesAsync();
            return new CloudFileInfo(blobClient, properties);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            // Check if it's a "folder" (by trying to list children)
            var folderCheck = containerClient.GetBlobsByHierarchyAsync(prefix: filePath, delimiter: "/");
            var folder = await folderCheck.FirstOrDefaultAsync(item => item.IsPrefix);
            if (folder == null)
                return new CloudFileInfo(blobClient, filePath);
            else 
                return new CloudFileInfo(blobClient, folder);
        }
    }

    public IAsyncEnumerable<ICloudFileInfo> GetDirectoryContentsAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        if (_container == null)
            throw new ArgumentNullException(nameof(_container), "An Azure blob storgae container name must be set before calling this method.");
        directoryPath = directoryPath.ToCloudFilePath();

        var containerClient = _blobServiceClient.GetBlobContainerClient(_container);
        return containerClient
            .GetBlobsByHierarchyAsync(prefix: directoryPath, delimiter: "/")
            .Select(blob => {
                var client = (blob.IsPrefix) 
                    ? containerClient.GetBlobClient(blob.Prefix) 
                    : containerClient.GetBlobClient(blob.Blob.Name);
                return new CloudFileInfo(client, blob);
            });
    }


    public async Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fileInfo = await GetFileInfoAsync(filePath);
        return fileInfo.Exists;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task DeleteAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        if (_container == null)
            throw new ArgumentNullException(nameof(_container), "An Azure blob storgae container name must be set before calling this method.");
        var containerClient = _blobServiceClient.GetBlobContainerClient(_container);
        var batchClient = _blobServiceClient.GetBlobBatchClient();

        var deleteUris = new List<Uri>();

        foreach (var path in paths)
        {
            var cleanPath = path.ToCloudFilePath();
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
                var blobUri = containerClient.GetBlobClient(path).Uri;
                deleteUris.Add(blobUri);
            }
        }

        // Perform batch delete
        await batchClient.DeleteBlobsAsync(deleteUris);
    }

}
