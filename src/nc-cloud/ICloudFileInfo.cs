/// <summary>
/// Interface for managing files store in a cloud provider.
/// </summary>
public interface ICloudFileInfo
{
    /// <summary>
    /// Name of the file.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Relative path to the file.
    /// </summary>
    string Path { get; }

    /// <summary>
    /// True if the file exists.
    /// </summary>
    bool Exists { get; }

    /// <summary>
    /// File length.
    /// </summary>
    long Length { get; }

    /// <summary>
    /// File content type.
    /// </summary>
    string? ContentType { get; }

    /// <summary>
    /// Last modification date of the file.
    /// </summary>
    DateTimeOffset? LastModified { get; }

    /// <summary>
    /// True if the file is a directory.
    /// </summary>
    bool IsDirectory { get; }

    /// <summary>
    /// Etag provided by underlying provider.
    /// </summary>
    string? ETag { get; }

    /// <summary>
    /// Metadata associated with file.
    /// </summary>
    IDictionary<string, string?> Metadata { get; }

    /// <summary>
    /// Opens a stream at the beginning of the file.
    /// </summary>
    Task<Stream> CreateReadStreamAsync();

    /// <summary>
    /// Writes to a file.
    /// </summary>
    Task WriteStreamAsync(Stream stream, string? contentType = null, IDictionary<string, string?>? metadata = null);
}
