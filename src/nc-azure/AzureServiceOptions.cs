/// <summary>
/// Options for wiring Azure services.
/// </summary>
public class AzureServiceOptions
{
    /// <summary>
    /// Options for wiring Azure Blob services.
    /// </summary>
    public IDictionary<string, CloudFileServiceOptions>? BlobStorage { get; set; }

    /// <summary>
    /// Options for wiring named <see cref="BlobSource">.
    /// </summary>
    public IDictionary<string, BlobSourceOptions>? BlobSources { get; set; }

    public IDictionary<string, CalendarSourceOptions>? CalendarSources { get; set; }
}
