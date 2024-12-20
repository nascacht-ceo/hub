using Microsoft.Extensions.Configuration;

/// <summary>
/// Extensions for adding Azure blob storage accounts to a <see cref="ICloudFileManager"/>.
/// </summary>
public static class CloudFileManagerExtensions
{
    public static ICloudFileManager AddBlobStorage(this ICloudFileManager manager, string name, CloudFileServiceOptions options)
    {
        return manager.Add(name, (_) => new CloudFileService(options));
    }

    public static ICloudFileManager AddBlobStorage(this ICloudFileManager manager, string name, IConfiguration configuration)
    {
        var options = new CloudFileServiceOptions();
        configuration.Bind(options);
        return manager.AddBlobStorage(name, options);
        
    }

    public static ICloudFileManager AddBlobStorage(this ICloudFileManager manager, string name, string storageAccount, string accessKey)
    {
        return manager.Add(name, (_) => new CloudFileService(new CloudFileServiceOptions() 
        {
            StorageAccount = storageAccount,
            AccessKey = accessKey
        }));
    }

    public static ICloudFileManager AddBlobStorage(this ICloudFileManager manager, string name, string connectionString)
    {
        return manager.Add(name, (_) => new CloudFileService(new CloudFileServiceOptions()
        {
            ConnectionString = connectionString
        }));
    }
}
