public interface ICloudFile
{
    /// <summary>
    /// True if the file exists in the repository.
    /// </summary>
    public bool Exists { get; }

    /// <summary>
    /// Length of the file.
    /// </summary>
    public long Length { get; }

    /// <summary>
    /// Path to the file from the root of the repository.
    /// </summary>
    public string? Path { get; }

    /// <summary>
    /// Name of the file.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Last time the file was modified.
    /// </summary>
    public DateTimeOffset? LastModified { get; }

    /// <summary>
    /// True if the file is a directory.
    /// </summary>
    public bool IsDirectory { get; }

    /// <summary>
    /// File content type.
    /// </summary>
    string? ContentType { get; }

    /// <summary>
    /// Etag provided by underlying provider.
    /// </summary>
    string? ETag { get; }

    /// <summary>
    /// Metadata associated with file.
    /// </summary>
    IDictionary<string, string?> Metadata { get; }

    /// <summary>
    /// Create a read stream for the file.
    /// </summary>
    public Task<Stream> CreateReadStreamAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a stream to the underlying repository.
    /// </summary>
    /// <param name="stream">Stream containing content to write.</param>
    public Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default);
}
