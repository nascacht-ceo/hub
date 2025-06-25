/// <summary>
/// Interface for managing <see cref="IRepository{T}"/> in a source.
/// </summary>
/// <typeparam name="T">Type stored in the repository.</typeparam>
/// <example>Manage tables in a source database.</example>
/// <example>Manage buckets in Amazon S3.</example>
/// <example>Manage accounts in an email server.</example>
public interface ISource<T> where T : class
{
    /// <summary>
    /// Searches for <see cref="IRepository{T}"> matching <paramref name="repositoryName"/>.
    /// </summary>
    /// <param name="repositoryName">Name of respository to search for; wildcard matching with * and ? are supported.</param>
    /// <param name="cancellationToken">CancellationToken. Defaults to <see cref="default"/></param>
    /// <returns>Instances of <see cref="IRepository{T}"/> matching <paramref name="repositoryName"/>.</returns>
    public IAsyncEnumerable<IRepository<T>> SearchAsync(IRepositoryOptions<T> options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an <see cref="IRepository{T}"/>
    /// </summary>
    /// <param name="options">Options to use when creating the <see cref="IRepository{T}"/></param>
    /// <param name="cancellationToken">CancellationToken. Defaults to <see cref="default"/></param>
    /// <returns>An <see cref="IRepository{T}"/>.</returns>
    public Task<IRepository<T>> CreateAsync(IRepositoryOptions<T> options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a named <see cref="IRepository{T}"/>
    /// </summary>
    /// <param name="repositoryName">Name of repository to delete.</param>
    /// <param name="cancellationToken">CancellationToken. Defaults to <see cref="default"/></param>
    public Task DeleteAsync(IRepositoryOptions<T> options, CancellationToken cancellationToken = default);
}
