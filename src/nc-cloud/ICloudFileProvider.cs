/// <summary>
/// Interface for managing files in a cloud environment.
/// Mimics most of netcore's <see cref="Microsoft.Extensions.IFileProvider"/>.
/// </summary>
public interface ICloudFileProvider
{
    /// <summary>
    /// The <see cref="ICloudFileInfo"/> about a file at <paramref name="filePath"/>.
    /// </summary>
    Task<ICloudFileInfo> GetFileInfoAsync(string filePath);

    /// <summary>
    /// Get the contents of a repository.
    /// </summary>
    Task<IAsyncEnumerable<ICloudFileInfo>> GetDirectoryContentsAsync(string directoryPath);

    /// <summary>
    /// Determine if a file existsin a provider at <paramref name="filePath"/>.
    /// </summary>
    Task<bool> FileExistsAsync(string filePath);

    /// <summary>
    /// Create a new drive in the cloud.
    /// </summary>
    /// <param name="name">Name of provider to create.</param>
    /// <param name="metadata">Metadata about the provider.</param>
    /// <returns><see cref="ICloudFileProvider"/> pointed to the newly created provider.</returns>
    Task<ICloudFileProvider> CreateAsync(string name, IDictionary<string, string>? metadata = null);

    /// <summary>
    /// Lists all available providers.
    /// </summary>
    /// <returns>An asynchronous stream of ICloudFileProvider instances scoped to each container or bucket.</returns>
    Task<IAsyncEnumerable<ICloudFileProvider>> ListAsync(string? name = null);
}
