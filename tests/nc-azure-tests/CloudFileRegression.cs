using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace nc.Azure.Tests
{
    public class CloudFileRegression
    {
        private readonly IConfigurationRoot _config;
        private readonly BlobServiceClient _blobService;
        private readonly CloudFileService _fileService;

        public CloudFileRegression()
        {
            _config = new ConfigurationBuilder()
                .AddUserSecrets("nc")
                .AddJsonFile("azure.json")
                .Build();

            _blobService = new BlobServiceClient($"DefaultEndpointsProtocol=https;AccountName={_config["Azure:BlobStorage:StorageAccountName"]};AccountKey={_config["Azure:BlobStorage:AccessKey"]};EndpointSuffix=core.windows.net");
            _fileService = new CloudFileService(_blobService);
        }

        [Fact]
        public async Task WiresServices()
        {
            // var manager = new CloudFileManager();

            var config = new ConfigurationBuilder()
                .AddJsonFile("azure.json")
                .Build();

            var options = new AzureServiceOptions();
            config.Bind(options);

            var services = new ServiceCollection()
                .AddNascachtAzureServices(config.GetSection("Azure"))
                .BuildServiceProvider();

            //services.AddKeyedSingleton<ICloudFileService>("one", (sp, key) => new CloudFileService(_blobService));
            //services.AddKeyedSingleton<ICloudFileService>("two", (sp, key) => new CloudFileService(_blobService));
            //var sp = services.BuildServiceProvider();

            //var cfs = sp.GetServices<ICloudFileService>();

            //var options = new CloudFileServiceOptions();
            //_config.GetSection("Azure:BlobStorage").Bind(options);
            //Assert.Equal(2, options.Count);
            //foreach (var kvp in options)
            //{
            //    services.AddSingleton<ICloudFileService>(sp => new CloudFileService(kvp.Key, kvp.Value));
            //}


            //var providers = await manager["test"].SearchAsync("*").ToListAsync();
            //Assert.NotEmpty(providers);
        }

        [Fact]
        public async Task Walkthrough()
        {
            var name = $"{Guid.NewGuid()}";
            var provider = await _fileService.CreateAsync(name);
            Assert.NotNull(provider);

            var file = "/sample/files/testing.txt";
            var info = await provider.GetFileInfoAsync(file);
            Assert.NotNull(info);
            Assert.False(info.IsDirectory);
            Assert.False(info.Exists);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Hello World."));
            await info.WriteStreamAsync(stream);


            // Accesses a file.
            info = await provider.GetFileInfoAsync(file);
            Assert.True(info.Exists);

            // Finds files in a folder.
            var contents = await provider.GetDirectoryContentsAsync("sample/files/").ToListAsync();
            Assert.NotEmpty(contents);
            Assert.False(contents[0].IsDirectory);

            // Finds a folder
            contents = await provider.GetDirectoryContentsAsync("sample/").ToListAsync();
            Assert.NotEmpty(contents);
            Assert.True(contents[0].IsDirectory);
            // Deletes a folder.
            await provider.DeleteAsync(["sample/files/"]);
            info = await provider.GetFileInfoAsync(file);
            Assert.False(info.Exists);

            await _fileService.DeleteAsync(name);

            var providers = await _fileService.SearchAsync("*").ToListAsync();
            Assert.DoesNotContain(providers, p => p.Name == name);
            // Assert.Empty(providers);

        }
    }
}
