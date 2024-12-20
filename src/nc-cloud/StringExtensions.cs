/// <summary>
/// String extension methods
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Cleans a <paramref name="filePath"/> to be used as a cloud file path.
    /// This replaces backslashes with forward slashes and removes leading slashes.
    /// </summary>
    public static string ToCloudFilePath(this string filePath)
    {
        filePath.Replace("\\", "/");
        if (filePath.StartsWith("/"))
            filePath = filePath.Substring(1);
        return filePath;
    }
}
