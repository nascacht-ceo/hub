using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow;

public interface IRepository<T> where T : class
{
    public IAsyncEnumerable<T> SearchAsync(IQuery<T> query, CancellationToken cancellationToken = default);

    public Task<T> SaveAsync(T instance, CancellationToken cancellationToken = default);

    public Task<T> DeleteAsync(T instance, CancellationToken cancellationToken = default);
}

public static class RepositoryExtensions
{
    public static ExecutionDataflowBlockOptions BlockOptions = new ExecutionDataflowBlockOptions
    {
        MaxDegreeOfParallelism = 4
    };

    public static async IAsyncEnumerable<T> SaveAsync<T>(this IRepository<T> repository, IEnumerable<T> instances, ExecutionDataflowBlockOptions? options = null) where T : class
    {
        options ??= BlockOptions;

        Func<T, Task<T>> body = async instance =>
        {
            return await repository.SaveAsync(instance, options.CancellationToken);
        };
        var block = new TransformBlock<T, T>(body, options);

        foreach (var instance in instances)
        {
            block.Post(instance);
        }

        block.Complete();

        while (await block.OutputAvailableAsync(options.CancellationToken))
        {
            yield return await block.ReceiveAsync(options.CancellationToken);
        }
    }

    public static async Task<T> DeleteAsync<T>(this IRepository<T> repository, T instance, CancellationToken cancellationToken = default) where T : class
    {
        return await repository.DeleteAsync(instance, cancellationToken);
    }
    public static async IAsyncEnumerable<T> SearchAsync<T>(this IRepository<T> repository, IQuery<T> query, [EnumeratorCancellation]CancellationToken cancellationToken = default) where T : class
    {
        await foreach (var item in repository.SearchAsync(query, cancellationToken))
        {
            yield return item;
        }
    }
}