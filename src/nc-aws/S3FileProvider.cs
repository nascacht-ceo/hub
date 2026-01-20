using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

/// <summary>
/// AWS S3 implementation of ICloudFileProvider.
/// </summary>
public class S3FileProvider : ICloudFileProvider
{
	/// <summary>
	/// S3 Client used to interact with the S3 service.
	/// </summary>
	public IAmazonS3 S3Client { get; private set; }
    private readonly string _bucketName;
    private readonly ILogger? _logger;

	/// <summary>
	/// Bucket name associated with this provider.
	/// </summary>
	public string Name => _bucketName;

	/// <summary>
	/// Constructor for S3FileProvider.
	/// </summary>
	public S3FileProvider(IAmazonS3 s3Client, string bucketName, ILogger? logger = null)
    {
        S3Client = s3Client;
        _bucketName = bucketName;
        _logger = logger;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task<ICloudFileInfo> GetFileInfoAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (_bucketName == null)
            throw new ArgumentNullException(nameof(_bucketName), "An S3 bucket name must be set before calling this method.");

        filePath = filePath.ToCloudFilePath();

        try
        {
            var metadataResponse = await S3Client.GetObjectMetadataAsync(_bucketName, filePath, cancellationToken);

            return new S3FileInfo(this, filePath, metadataResponse);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Check if the path represents a "folder" by listing objects with the prefix
            var listResponse = await S3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = filePath,
                Delimiter = "/"
            }, cancellationToken);

            if (listResponse.CommonPrefixes.Any() || listResponse.S3Objects.Any())
            {
                return new S3FileInfo(this, listResponse.S3Objects.First());
            }
            else
            {
                return new S3FileInfo(this, filePath);
            }
        }
    }

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public IAsyncEnumerable<ICloudFileInfo> GetDirectoryContentsAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        if (_bucketName == null)
            throw new ArgumentNullException(nameof(_bucketName), "An S3 bucket name must be set before calling this method.");

        directoryPath = directoryPath.ToCloudFilePath();

        return GetS3ContentsAsync(directoryPath, cancellationToken);
    }

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	private async IAsyncEnumerable<ICloudFileInfo> GetS3ContentsAsync(string directoryPath, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var listRequest = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = directoryPath,
            Delimiter = "/"
        };

        ListObjectsV2Response listResponse;
        do
        {
            listResponse = await S3Client.ListObjectsV2Async(listRequest, cancellationToken);

            // Yield folders (common prefixes)
            foreach (var prefix in listResponse.CommonPrefixes)
            {
                yield return new S3FileInfo(this, prefix);
            }

            // Yield files
            foreach (var s3Object in listResponse.S3Objects)
            {
                yield return new S3FileInfo(this, s3Object);
            }

            listRequest.ContinuationToken = listResponse.NextContinuationToken;

        } while (listResponse.IsTruncated ?? false);
    }

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public async Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fileInfo = await GetFileInfoAsync(filePath, cancellationToken);
        return fileInfo.Exists;
    }

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public async Task DeleteAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        if (_bucketName == null)
            throw new ArgumentNullException(nameof(_bucketName), "An S3 bucket name must be set before calling this method.");

        foreach (var path in paths)
        {
            var cleanPath = path.ToCloudFilePath();

            if (cleanPath.EndsWith("/"))
            {
                // Recursively delete all objects in the folder
                var objectsToDelete = new List<KeyVersion>();
                var listRequest = new ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    Prefix = cleanPath
                };

                ListObjectsV2Response listResponse;
                do
                {
                    listResponse = await S3Client.ListObjectsV2Async(listRequest, cancellationToken);
                    objectsToDelete.AddRange(listResponse.S3Objects.Select(obj => new KeyVersion { Key = obj.Key }));

                    listRequest.ContinuationToken = listResponse.NextContinuationToken;

                } while (listResponse.IsTruncated ?? false);

                if (objectsToDelete.Any())
                {
                    await S3Client.DeleteObjectsAsync(new DeleteObjectsRequest
                    {
                        BucketName = _bucketName,
                        Objects = objectsToDelete
                    }, cancellationToken);
                }
            }
            else
            {
                // Delete single file
                await S3Client.DeleteObjectAsync(_bucketName, cleanPath, cancellationToken);
            }
        }
    }
}
