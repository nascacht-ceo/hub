
public class LocalFile : ICloudFile
{
    private readonly FileInfo _info;

    public LocalFile(string path, IDictionary<string, string?>? metadata = null)
        : this(new FileInfo(path), metadata)
    {
    }

    public LocalFile(FileInfo info, IDictionary<string, string?>? metadata = null)
    {
        _info = info;
        Metadata = metadata ?? new Dictionary<string, string?>();
    }

    public bool Exists => _info.Exists;

    public long Length => _info.Length;

    public string? Path => _info.FullName;

    public string Name => _info.Name;

    public DateTimeOffset? LastModified => _info.LastWriteTime;

    public bool IsDirectory => false;

    public string? ContentType => throw new NotImplementedException();

    public string? ETag => throw new NotImplementedException();

    public IDictionary<string, string?> Metadata { get; set; }

    public Task<Stream> CreateReadStreamAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(File.OpenRead(_info.FullName) as Stream);
    }

    public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        await stream.CopyToAsync(File.OpenWrite(_info.FullName), cancellationToken);
    }
}
