using System.Collections.Concurrent;

/// <summary>
/// Options class for <see cref="CloudFileManager"/>.
/// </summary>
public class CloudFileManagerOptions
{
    /// <summary>
    /// Named <see cref="ICloudFileService"> to add to <see cref="CloudFileManager"/>.
    /// </summary>
    public ConcurrentDictionary<string, Func<IServiceProvider, ICloudFileService>> ServiceFactories { get; set; } 
        = new ConcurrentDictionary<string, Func<IServiceProvider, ICloudFileService>>();
}

