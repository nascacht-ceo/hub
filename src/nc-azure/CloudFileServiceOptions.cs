using Azure.Storage.Blobs;


/// <summary>
/// Options to use when constructing a <see cref="CloudFileService"/>.
/// </summary>
public class CloudFileServiceOptions
{
    /// <summary>
    /// Storage account name.
    /// </summary>
    public string? StorageAccount { get; set; }

    /// <summary>
    /// Storage account access key.
    /// </summary>
    public string? AccessKey { get; set; }

    private string? _connectionString;

    /// <summary>
    /// Storage account connection string.
    /// </summary>
    public string? ConnectionString 
    {
        get 
        {
            if (_connectionString != null)
                return _connectionString;
            if (StorageAccount == null || AccessKey == null)
                throw new ArgumentNullException("Either ConnectionString or SourceAccount and AccessKey must be set.");
            return $"DefaultEndpointsProtocol=https;AccountName={StorageAccount};AccountKey={AccessKey};EndpointSuffix=core.windows.net"; } 
        set { _connectionString = value; } 
    }

    public BlobClientOptions? BlobClientOptions { get; set; } = new BlobClientOptions();
}
