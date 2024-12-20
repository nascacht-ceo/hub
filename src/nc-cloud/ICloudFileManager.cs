/// <summary>
/// Manager for all registered instances of <see cref="ICloudFileService"/>.
/// </summary>
public interface ICloudFileManager
{
    /// <summary>
    /// Enumerated list of nameed <see cref="ICloudFileService"/>.
    /// </summary>
    public IEnumerable<string> Keys { get; }

    /// <summary>
    /// Access a registered <see cref="ICloudFileService"/> by name.
    /// </summary>
    /// <param name="name">Registered name of <see cref="ICloudFileService"/>.</param>
    /// <returns><see cref="ICloudFileService"/>, or <see cref="ArgumentOutOfRangeException"/></returns>
    public ICloudFileService this[string name] { get; }

    /// <summary>
    /// Adds a named <see cref="ICloudFileService"/>/
    /// </summary>
    /// <param name="name">Name of service.</param>
    /// <param name="factory">Factory to instantiate service.</param>
    /// <returns><see cref="ICloudFileManager"/> for fuild coding.</returns>
    public ICloudFileManager Add(string name, Func<IServiceProvider, ICloudFileService> factory);


    /// <summary>
    /// Removes a named <see cref="ICloudFileService"/>.
    /// </summary>
    /// <returns><see cref="ICloudFileManager"/> for fuild coding.</returns>
    public ICloudFileManager Remove(string name);
}
