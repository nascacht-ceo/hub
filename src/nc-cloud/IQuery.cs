
public interface IQuery<T>
{
}

public interface ICloudFileQuery: IQuery<ICloudFile>
{
    public string Path { get; set; }

    public string Pattern { get; set; }

    public bool Recurse { get; set; }

    public string FolderDelimiter { get; set; }
}

public class CloudFileQuery: ICloudFileQuery
{
    public string Path { get; set; } = string.Empty;

    public string Pattern { get; set; } = "*";

    public bool Recurse { get; set; } = false;

    public string FolderDelimiter { get; set; } = "/";
}
