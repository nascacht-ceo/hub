using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

/// <summary>
/// Implement <see cref="ICloudFileService"/> for AWS S3.
/// </summary>
public class S3FileService : ICloudFileService
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger? _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    public S3FileService(S3FileServiceOptions options, ILogger? logger = null)
    {
        _s3Client = options.CreateServiceClient<IAmazonS3>();
        _logger = logger;
    }

    public S3FileService(IAmazonS3 s3Client, ILogger? logger = null)
    {
        _s3Client = s3Client;
        _logger = logger;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task<ICloudFileProvider> CreateAsync(string name, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var putBucketRequest = new PutBucketRequest
            {
                BucketName = name
            };

            await _s3Client.PutBucketAsync(putBucketRequest, cancellationToken);

            if (metadata != null)
            {
                var putBucketTaggingRequest = new PutBucketTaggingRequest
                {
                    BucketName = name,
                    TagSet = metadata.Select(kv => new Tag { Key = kv.Key, Value = kv.Value }).ToList()
                };

                await _s3Client.PutBucketTaggingAsync(putBucketTaggingRequest, cancellationToken);
            }

            return new S3FileProvider(_s3Client, name, _logger);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating an S3 bucket.");
            throw;
        }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async IAsyncEnumerable<ICloudFileProvider> SearchAsync(string name, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var listBucketsResponse = await _s3Client.ListBucketsAsync(cancellationToken);
        foreach (var bucket in listBucketsResponse.Buckets)
        {
            if (bucket.BucketName.IsWildcardMatch(name))
            {
                yield return new S3FileProvider(_s3Client, bucket.BucketName, _logger);
            }
        }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task DeleteAsync(string bucketName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(bucketName))
            throw new ArgumentNullException(nameof(bucketName), "You must specify a bucket name to delete.");

        try
        {
            var deleteBucketRequest = new DeleteBucketRequest
            {
                BucketName = bucketName
            };

            await _s3Client.DeleteBucketAsync(deleteBucketRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting an S3 bucket.");
            throw;
        }
    }
}
