using Amazon.S3;
using Amazon.S3.Model;

public class S3FileInfo : ICloudFileInfo
{
    private readonly S3FileProvider _provider;

    public string Name { get; set; }
    public string Path { get; set; }
    public bool Exists { get; set; }
    public long Length { get; set; }
    public string? ContentType { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public bool IsDirectory { get; set; }
    public string? ETag { get; private set; }
    public IDictionary<string, string?> Metadata { get; set; }

    /// <summary>
    /// Constructor for existing files or directories.
    /// </summary>
    public S3FileInfo(S3FileProvider provider, S3Object s3Object)
    {
        _provider = provider;
        Name = s3Object.Key.Split('/').Last();
        Path = s3Object.Key;
        Exists = true;
        IsDirectory = s3Object.Key.EndsWith("/");
        Length = s3Object.Size ?? 0;
        LastModified = s3Object.LastModified;
        Metadata = new Dictionary<string, string?>(); // Metadata is not included in S3Object, needs a separate call
        ETag = s3Object.ETag;
    }

    /// <summary>
    /// Constructor for files that don't exist.
    /// </summary>
    public S3FileInfo(S3FileProvider provider, string path, GetObjectMetadataResponse metadataResponse)
    {
        _provider = provider;
        Name = path.Split('/').Last();
        Exists = true;
        Path = path;
        IsDirectory = path.EndsWith("/");
        Metadata = new Dictionary<string, string?>();
    }

    /// <summary>
    /// Constructor for files that don't exist.
    /// </summary>
    public S3FileInfo(S3FileProvider provider, string path)
    {
        _provider = provider;
        Name = path.Split('/').Last();
        Path = path;
        Exists = false;
        IsDirectory = path.EndsWith("/");
        Metadata = new Dictionary<string, string?>();
    }

    /// <summary>
    /// Create a read stream for the file.
    /// </summary>
    public async Task<Stream> CreateReadStreamAsync()
    {
        var response = await _provider.S3Client.GetObjectAsync(_provider.Name, Path);
        return response.ResponseStream;
    }

    /// <summary>
    /// Write a stream to the file.
    /// </summary>
    public async Task WriteStreamAsync(Stream stream, string? contentType = null, IDictionary<string, string?>? metadata = null)
    {
        var putRequest = new PutObjectRequest
        {
            BucketName = _provider.Name,
            Key = Path,
            InputStream = stream,
            ContentType = contentType
        };

        // Add metadata
        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                putRequest.Metadata.Add(kvp.Key, kvp.Value);
            }
        }

        await _provider.S3Client.PutObjectAsync(putRequest);
    }
}
