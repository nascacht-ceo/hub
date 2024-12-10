using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;

public class CloudFileInfo : ICloudFileInfo
{
    private readonly BlobClient _blobClient;
    private readonly BlobProperties? _properties;
    private readonly BlobItemProperties? _itemProperties;

    public CloudFileInfo(BlobClient blobClient, bool exists = true)
    {
        _blobClient = blobClient;
        Exists = exists;
        Name = blobClient.Name.Split('/').Last();
        Path = blobClient.Name;
        IsDirectory = false; // Azure Blob Storage does not have real directories
    }

    public CloudFileInfo(BlobClient blobClient, BlobProperties properties, bool exists = true)
        : this(blobClient, exists)
    {
        _properties = properties;
        Length = properties?.ContentLength ?? 0;
        ContentType = properties?.ContentType;
        LastModified = properties?.LastModified;
        IsDirectory = false; // Azure Blob Storage does not have real directories
        Metadata = properties?.Metadata ?? new Dictionary<string, string>();
    }

    public CloudFileInfo(BlobClient blobClient, BlobItemProperties properties, bool exists = true)
        : this(blobClient, exists)
    {
        _itemProperties = properties;
        Length = properties.ContentLength ?? 0;
        ContentType = properties.ContentType;
        LastModified = properties.LastModified;
        IsDirectory = false; 
        Metadata = ExtractAdditionalProperties(properties);
    }

    public static IDictionary<string, string?> ExtractAdditionalProperties(BlobItemProperties properties)
    {
        var additionalProperties = new Dictionary<string, string?>();

        if (!string.IsNullOrEmpty(properties.ContentEncoding))
            additionalProperties["ContentEncoding"] = properties.ContentEncoding;

        if (!string.IsNullOrEmpty(properties.CacheControl))
            additionalProperties["CacheControl"] = properties.CacheControl;

        if (!string.IsNullOrEmpty(properties.ContentLanguage))
            additionalProperties["ContentLanguage"] = properties.ContentLanguage;

        if (!string.IsNullOrEmpty(properties.ContentDisposition))
            additionalProperties["ContentDisposition"] = properties.ContentDisposition;

        if (properties.ETag != null)
            additionalProperties["ETag"] = properties.ETag.ToString();

        if (properties.LeaseStatus != null)
            additionalProperties["LeaseStatus"] = properties.LeaseStatus.ToString();

        if (properties.LeaseState != null)
            additionalProperties["LeaseState"] = properties.LeaseState.ToString();

        if (properties.LeaseDuration != null)
            additionalProperties["LeaseDuration"] = properties.LeaseDuration.ToString();

        return additionalProperties;
    }

    public static IDictionary<string, string?> ExtractAdditionalProperties(BlobProperties properties)
    {
        var additionalProperties = new Dictionary<string, string?>();

        // Add additional properties
        if (!string.IsNullOrEmpty(properties.ContentEncoding))
            additionalProperties["ContentEncoding"] = properties.ContentEncoding;

        if (!string.IsNullOrEmpty(properties.CacheControl))
            additionalProperties["CacheControl"] = properties.CacheControl;

        if (!string.IsNullOrEmpty(properties.ContentLanguage))
            additionalProperties["ContentLanguage"] = properties.ContentLanguage;

        if (!string.IsNullOrEmpty(properties.ContentDisposition))
            additionalProperties["ContentDisposition"] = properties.ContentDisposition;

        if (properties.ContentHash != null)
            additionalProperties["ContentMD5"] = Convert.ToBase64String(properties.ContentHash);

        additionalProperties["ETag"] = properties.ETag.ToString();

        additionalProperties["LeaseStatus"] = properties.LeaseStatus.ToString();

        additionalProperties["LeaseState"] = properties.LeaseState.ToString();

        additionalProperties["LeaseDuration"] = properties.LeaseDuration.ToString();

        // Add metadata
        foreach (var metadata in properties.Metadata)
        {
            additionalProperties[metadata.Key] = metadata.Value;
        }

        return additionalProperties;
    }


    public string Name { get; }
    public string Path { get; }
    public bool Exists { get; }
    public long Length { get; }
    public string? ContentType { get; }
    public DateTimeOffset? LastModified { get; }
    public bool IsDirectory { get; }
    public IDictionary<string, string?> Metadata { get; }

    public async Task<Stream> CreateReadStreamAsync()
    {
        var response = await _blobClient.DownloadStreamingAsync();
        return response.Value.Content;
    }

    public async Task WriteStreamAsync(Stream stream, string contentType, IDictionary<string, string?>? metadata = null)
    {
        var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };
        await _blobClient.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = blobHttpHeaders,
            Metadata = metadata
        });
    }

    public async Task<IDictionary<string, string>> GetMetadataAsync()
    {
        var properties = await _blobClient.GetPropertiesAsync();
        return properties.Value.Metadata;
    }
}
