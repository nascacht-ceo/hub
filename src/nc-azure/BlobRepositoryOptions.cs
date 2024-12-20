

public class BlobRepositoryOptions : IRepositoryOptions<ICloudFile>
{
    /// <summary>
    /// Name of the repository.
    /// </summary>
    public string Name { get; set; }
    public IDictionary<string, string?> Metadata { get; internal set; }

    public BlobRepositoryOptions(string name)
    {
        Name = name;
        Metadata = new Dictionary<string, string?>();
    }

    public static implicit operator BlobRepositoryOptions(string name)
    {
        return new BlobRepositoryOptions(name);
    }
}
