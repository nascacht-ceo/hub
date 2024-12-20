using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

public class CloudFileManager : ICloudFileManager
{
    private readonly IServiceScopeFactory _factory;
    private readonly CloudFileManagerOptions _options;
    ConcurrentDictionary<string, ICloudFileService> _services = new ConcurrentDictionary<string, ICloudFileService>();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="options"></param>
    public CloudFileManager(IOptions<CloudFileManagerOptions> options, IServiceScopeFactory factory)
    {
        _factory = factory;
        _options = options.Value;
    }

    public IEnumerable<string> Keys => _options.ServiceFactories.Keys;

    public ICloudFileService this[string name]
    {
        get
        {
            if (_services.TryGetValue(name, out var service))
            {
                return service;
            }
            if (_options.ServiceFactories.TryGetValue(name, out var factory))
            {
                using var scope = _factory.CreateScope();
                try
                {
                    var instance = factory(scope.ServiceProvider);
                    _services.TryAdd(name, instance);
                    return instance;

                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to create service {name}.", ex);
                }
            }
            throw new ArgumentOutOfRangeException(nameof(name) , name);
        }
    }

    /// <summary>
    /// Adds a named <see cref="ICloudFileService"/>
    /// </summary>
    public ICloudFileManager Add(string name, Func<IServiceProvider, ICloudFileService> factory)
    {
        _services.TryRemove(name, out var _);
        _options.ServiceFactories.AddOrUpdate(name, factory, (key, oldValue) => factory);
        return this;
    }

    /// <summary>
    /// Removes a named <see cref="ICloudFileService"/>.
    /// </summary>
    public ICloudFileManager Remove(string name)
    {
        _options.ServiceFactories.TryRemove(name, out var _);
        _services.TryRemove(name, out var _);
        return this;
    }
}
