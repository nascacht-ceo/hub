/// <summary>
/// Interface for use by <see cref="ISource.CreateAsync(IRepositoryOptions, CancellationToken)"/>
/// </summary>
public interface IRepositoryOptions<T> where T : class
{
    /// <summary>
    /// Name of the source.
    /// </summary>
    public string Name { get; }
}
