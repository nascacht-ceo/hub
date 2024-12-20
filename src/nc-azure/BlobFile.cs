using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;

public class BlobFile : ICloudFile
{
    public BlobFile(BlobClient blobClient, BlobHierarchyItem item)
    {
        _blobClient = blobClient;
        IsDirectory = !item.IsBlob;
        Exists = true;
        Length = 0;
        Metadata = new Dictionary<string, string?>();
        if (item.IsBlob)
        {
            Name = new FileInfo(item.Blob.Name).Name;
            Path = item.Blob.Name;
            Length = item.Blob.Properties.ContentLength ?? 0;
            ContentType = item.Blob.Properties.ContentType;
            LastModified = item.Blob.Properties.LastModified;
            ETag = item.Blob.Properties.ETag.ToString();
            AddBlobProperties(item.Blob.Properties);
            Metadata = item.Blob.Metadata;
        }
        else
        {
            Name = new DirectoryInfo(item.Prefix).Name;
            Path = item.Prefix;
        }
    }

    public BlobFile(BlobClient client, BlobContentInfo info)
    {
        _blobClient = client;
        IsDirectory = false; 
        Exists = true;
        LastModified = info.LastModified;
        Metadata = new Dictionary<string, string?>();
        ETag = info.ETag.ToString();
    }

    private IDictionary<string, string?> AddBlobProperties(BlobItemProperties properties)
    {
        if (!string.IsNullOrEmpty(properties.ContentEncoding))
            Metadata["ContentEncoding"] = properties.ContentEncoding;

        if (!string.IsNullOrEmpty(properties.CacheControl))
            Metadata["CacheControl"] = properties.CacheControl;

        if (!string.IsNullOrEmpty(properties.ContentLanguage))
            Metadata["ContentLanguage"] = properties.ContentLanguage;

        if (!string.IsNullOrEmpty(properties.ContentDisposition))
            Metadata["ContentDisposition"] = properties.ContentDisposition;

        if (properties.ETag != null)
            Metadata["ETag"] = properties.ETag.ToString();

        if (properties.LeaseStatus != null)
            Metadata["LeaseStatus"] = properties.LeaseStatus.ToString();

        if (properties.LeaseState != null)
            Metadata["LeaseState"] = properties.LeaseState.ToString();

        if (properties.LeaseDuration != null)
            Metadata["LeaseDuration"] = properties.LeaseDuration.ToString();

        return Metadata;

    }

    public bool Exists { get; set; }

    public long Length { get; set; }

    public string? Path { get; set; }

    public string Name { get; set; }

    public DateTimeOffset? LastModified { get; set; }

    private readonly BlobClient _blobClient;

    public bool IsDirectory { get; set; }

    /// <summary>
    /// File content type.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Etag provided by underlying provider.
    /// </summary>
    public string? ETag { get; set; }

    /// <summary>
    /// Metadata associated with file.
    /// </summary>
    public IDictionary<string, string?> Metadata { get; set; }

    public async Task<Stream> CreateReadStreamAsync(CancellationToken cancellationToken = default)
    {
        var response = await _blobClient.DownloadStreamingAsync();
        return response.Value.Content;
    }

    public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var blobHttpHeaders = new BlobHttpHeaders { ContentType = ContentType };
        await _blobClient.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = blobHttpHeaders,
            Metadata = Metadata
        });
    }
}
