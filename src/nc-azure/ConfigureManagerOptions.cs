using Microsoft.Extensions.Options;

namespace nc.Azure
{
    public class ConfigureManagerOptions : IConfigureOptions<CloudFileManagerOptions>
    {
        private readonly AzureServiceOptions _azureServiceOptions;

        public ConfigureManagerOptions(IOptions<AzureServiceOptions> options)
        {
            _azureServiceOptions = options.Value;
        }

        public void Configure(CloudFileManagerOptions options)
        {
            if (_azureServiceOptions.BlobStorage != null)
            {
                foreach (var option in _azureServiceOptions.BlobStorage)
                {
                    Func<IServiceProvider, ICloudFileService> factory = (_) => new CloudFileService(option.Value);
                    options.ServiceFactories.AddOrUpdate(option.Key, factory, (key, oldValue) => factory);
                }
            }
        }
    }
}
