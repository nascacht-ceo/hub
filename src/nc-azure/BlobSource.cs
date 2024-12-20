using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

public class BlobSource : ISource<ICloudFile>
{
    private readonly BlobSourceOptions _storageOptions;
    private readonly ILogger? _logger;
    private readonly BlobServiceClient _blobServiceClient;

    public BlobSource(BlobSourceOptions storageOptions, ILogger? logger = null)
    {
        _storageOptions = storageOptions;
        _logger = logger;

        if (_storageOptions.ConnectionString != null)
            _blobServiceClient = new BlobServiceClient(_storageOptions.ConnectionString, _storageOptions.BlobClientOptions);
        else
            throw new ArgumentOutOfRangeException(nameof(storageOptions), "Either ConnectionString or StorgaeAccount and AccessKey must be set.");
    }

    public async Task<IRepository<ICloudFile>> CreateAsync(IRepositoryOptions<ICloudFile> options, CancellationToken cancellationToken = default)
    {
        var blobOptions = options as BlobRepositoryOptions;
        if (blobOptions == null)
            throw new ArgumentOutOfRangeException(nameof(options), "Options must be of type BlobRepositoryOptions.");
        var containerClient = _blobServiceClient.GetBlobContainerClient(options.Name);
        await containerClient.CreateIfNotExistsAsync(metadata: blobOptions.Metadata);
        // return new CloudFileProvider(_blobServiceClient, blobOptions.Name, _logger);
        return new BlobRepository(_blobServiceClient, options.Name);
    }


    public Task DeleteAsync(string repositoryName, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IRepository<ICloudFile>> SearchAsync(string repositoryName, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
