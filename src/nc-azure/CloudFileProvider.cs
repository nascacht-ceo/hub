using Azure.Storage.Blobs;

public class CloudFileProvider : ICloudFileProvider
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _blobContainerClient;
    private readonly string _container;

    public CloudFileProvider(BlobServiceClient blobServiceClient, string container)
    {
        _blobServiceClient = blobServiceClient;
        _blobContainerClient = blobServiceClient.GetBlobContainerClient(container);
        _container = container;
    }

    public async Task<ICloudFileInfo> GetFileInfoAsync(string filePath)
    {
        var containerName = GetContainerNameFromPath(filePath);
        var blobName = GetBlobNameFromPath(filePath);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        var blobClient = containerClient.GetBlobClient(blobName);
        if (await blobClient.ExistsAsync())
        {
            var properties = await blobClient.GetPropertiesAsync();
            return new CloudFileInfo(blobClient, properties.Value);
        }

        return new CloudFileInfo(blobClient, exists: false);
    }

    public async Task<IAsyncEnumerable<ICloudFileInfo>> GetDirectoryContentsAsync(string directoryPath)
    {
        var prefix = GetBlobNameFromPath(directoryPath);
        var containerClient = _blobServiceClient.GetBlobContainerClient(_container);

        return containerClient.GetBlobsAsync(prefix: prefix)
            .Select(blobItem => new CloudFileInfo(containerClient.GetBlobClient(blobItem.Name), blobItem.Properties));
    }

    public async Task<bool> FileExistsAsync(string filePath)
    {
        var fileInfo = await GetFileInfoAsync(filePath);
        return fileInfo.Exists;
    }

    public async Task<ICloudFileProvider> CreateAsync(string name, IDictionary<string, string>? metadata = null)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(name);
        await containerClient.CreateIfNotExistsAsync(metadata: metadata);
        return new CloudFileProvider(_blobServiceClient, name);
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task<IAsyncEnumerable<ICloudFileProvider>> ListAsync(string? name)
    {
        var containers = _blobServiceClient.GetBlobContainersAsync();

        var filteredContainers = containers
            .Where(container => container.Name.IsWildcardMatch(name));

        return filteredContainers
            .Select(container => new CloudFileProvider(_blobServiceClient, container.Name))
            .AsAsyncEnumerable();
    }

    private static string GetContainerNameFromPath(string path)
    {
        // Assumes the container name is the first segment of the path
        return path.Split('/')[0];
    }

    private static string GetBlobNameFromPath(string path)
    {
        // Assumes everything after the first segment is the blob name
        return string.Join('/', path.Split('/').Skip(1));
    }
}
