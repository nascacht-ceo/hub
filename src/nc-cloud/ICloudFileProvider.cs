/// <summary>
/// Interface for managing files in a cloud environment.
/// Mimics most of netcore's <see cref="Microsoft.Extensions.IFileProvider"/>.
/// </summary>
public interface ICloudFileProvider
{
    /// <summary>
    /// Name of the remote directory.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The <see cref="ICloudFileInfo"/> about a file at <paramref name="filePath"/>.
    /// </summary>
    Task<ICloudFileInfo> GetFileInfoAsync(string filePath, CancellationToken token = default);

    /// <summary>
    /// Get the contents of a repository.
    /// </summary>
    IAsyncEnumerable<ICloudFileInfo> GetDirectoryContentsAsync(string directoryPath, CancellationToken token = default);

    /// <summary>
    /// Determine if a file existsin a provider at <paramref name="filePath"/>.
    /// </summary>
    Task<bool> FileExistsAsync(string filePath, CancellationToken token = default);

    /// <summary>
    /// Deletes files or folders.
    /// </summary>
    /// <param name="paths">An enumeration of files or folder to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DeleteAsync(IEnumerable<string> paths, CancellationToken token = default);
}
