/// <summary>
/// Manage <see cref="ICloudFileProvider"/> in a cloud service.
/// </summary>
public interface ICloudFileService
{
    /// <summary>
    /// Create a new <see cref="ICloudFileProvider">in the cloud.
    /// </summary>
    /// <param name="name">Name of provider to create.</param>
    /// <param name="metadata">Metadata about the provider.</param>
    /// <returns><see cref="ICloudFileProvider"/> pointed to the newly created provider.</returns>
    Task<ICloudFileProvider> CreateAsync(string name, IDictionary<string, string>? metadata = null, CancellationToken token = default);

    /// <summary>
    /// Searches for <see cref="ICloudFileProvider"> matching <paramref name="pattern"/>.
    /// </summary>
    /// <returns>An asynchronous stream of ICloudFileProvider instances scoped to each container or bucket.</returns>
    IAsyncEnumerable<ICloudFileProvider> SearchAsync(string pattern, CancellationToken token = default);

    /// <summary>
    /// Deletes a <see cref="ICloudFileProvider">, including all files within the provider.
    /// </summary>
    /// <param name="name">Name of the provider to delete.</param>
    Task DeleteAsync(string name, CancellationToken token = default);
}
