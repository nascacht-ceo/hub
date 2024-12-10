public interface ICloudFileInfo
{
    string Name { get; }
    string Path { get; }
    bool Exists { get; }
    long Length { get; }
    string? ContentType { get; }
    DateTimeOffset? LastModified { get; }
    bool IsDirectory { get; }
    IDictionary<string, string?> Metadata { get; }

    Task<Stream> CreateReadStreamAsync();
    Task WriteStreamAsync(Stream stream, string contentType, IDictionary<string, string?>? metadata = null);
}
