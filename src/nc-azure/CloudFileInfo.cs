using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

public class CloudFileInfo : ICloudFileInfo
{
    private readonly BlobClient _blobClient;
    private readonly BlobProperties? _properties;
    private readonly BlobItemProperties? _itemProperties;

    /// <summary>
    /// Creates a <see cref="ICloudFileInfo"/> from <paramref name="item"/>.
    /// </summary>
    public CloudFileInfo(BlobClient blobClient, BlobHierarchyItem item)
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

    /// <summary>
    /// Constructor used from <see cref="CloudFileProvider.GetFileInfoAsync(string)"/>
    /// </summary>
    public CloudFileInfo(BlobClient blobClient, BlobProperties properties)
    {
        _blobClient = blobClient;
        Name = blobClient.Name.Split('/').Last();
        Path = blobClient.Name;
        IsDirectory = false; // Azure Blob Storage does not have real directories
        Exists = true;
        Length = properties.ContentLength;// ?? 0;
        ContentType = properties.ContentType;
        LastModified = properties.LastModified;
        Metadata = new Dictionary<string, string?>();
        ETag = properties.ETag.ToString();
        AddProperties(properties);
    }

    /// <summary>
    /// Constructor for files that don't exist, in case we want to write to them.
    /// </summary>
    public CloudFileInfo(BlobClient blobClient, string filePath)
    {
        _blobClient = blobClient;
        Name = filePath.Split('/').Last();
        Path = filePath;
        Exists = false;
        IsDirectory = false;
        Metadata = new Dictionary<string, string?>();
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
   
    //public CloudFileInfo(BlobClient blobClient, BlobProperties properties, bool exists = true)
    //    : this(blobClient, exists)
    //{
    //    _properties = properties;
    //    Length = properties?.ContentLength ?? 0;
    //    ContentType = properties?.ContentType;
    //    LastModified = properties?.LastModified;
    //    IsDirectory = false; // Azure Blob Storage does not have real directories
    //    Metadata = properties?.Metadata ?? new Dictionary<string, string>();
    //}

    //public CloudFileInfo(BlobClient blobClient, BlobItemProperties properties, bool exists = true)
    //    : this(blobClient, exists)
    //{
    //    _itemProperties = properties;
    //    Length = properties.ContentLength ?? 0;
    //    ContentType = properties.ContentType;
    //    LastModified = properties.LastModified;
    //    IsDirectory = false;
    //    Metadata = ExtractAdditionalProperties(properties);
    //}

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

    private IDictionary<string, string?> AddProperties(BlobProperties properties)
    {
        // Add additional properties
        if (!string.IsNullOrEmpty(properties.ContentEncoding))
            Metadata["ContentEncoding"] = properties.ContentEncoding;

        if (!string.IsNullOrEmpty(properties.CacheControl))
            Metadata["CacheControl"] = properties.CacheControl;

        if (!string.IsNullOrEmpty(properties.ContentLanguage))
            Metadata["ContentLanguage"] = properties.ContentLanguage;

        if (!string.IsNullOrEmpty(properties.ContentDisposition))
            Metadata["ContentDisposition"] = properties.ContentDisposition;

        if (properties.ContentHash != null)
            Metadata["ContentMD5"] = Convert.ToBase64String(properties.ContentHash);

        Metadata["LeaseStatus"] = properties.LeaseStatus.ToString();

        Metadata["LeaseState"] = properties.LeaseState.ToString();

        Metadata["LeaseDuration"] = properties.LeaseDuration.ToString();

        // Add Metadata
        foreach (var kvp in properties.Metadata)
        {
            Metadata[kvp.Key] = kvp.Value;
        }

        return Metadata;
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public string Path { get; }
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool Exists { get; }
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public long Length { get; }
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public string? ContentType { get; }
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public DateTimeOffset? LastModified { get; }
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool IsDirectory { get; }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public string? ETag { get; private set; }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public IDictionary<string, string?> Metadata { get; }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task<Stream> CreateReadStreamAsync()
    {
        var response = await _blobClient.DownloadStreamingAsync();
        return response.Value.Content;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task WriteStreamAsync(Stream stream, string? contentType = null, IDictionary<string, string?>? Metadata = null)
    {
        var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };
        await _blobClient.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = blobHttpHeaders,
            Metadata = Metadata
        });
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task<IDictionary<string, string>> GetMetadataAsync()
    {
        var properties = await _blobClient.GetPropertiesAsync();
        return properties.Value.Metadata;
    }
}
