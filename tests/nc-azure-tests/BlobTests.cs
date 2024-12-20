using Microsoft.Extensions.Configuration;

public class BlobTests
{
    public class BlobRepositoryTests : BlobTests
    {
        [Fact]
        public async Task Walkthrough()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("artifacts/azure.json")
                .Build();
            var options = new AzureServiceOptions();
            config.GetSection("Azure").Bind(options);
            Assert.NotNull(options.BlobSources);
            Assert.NotEmpty(options.BlobSources);
            Assert.Contains("documents", options.BlobSources.Keys);

            // Connect to a "computer": an Azure Blob Storage account.
            var blobSource = new BlobSource(options.BlobSources["documents"]);

            // Create a "drive": a container in the Blob Storage account.
            var name = $"{Guid.NewGuid()}";
            var repository = await blobSource.CreateAsync(new BlobRepositoryOptions(name));
            Assert.NotNull(repository);

            // Create a "file" in the "drive".
            var results = await repository.SaveAsync(new LocalFile("artifacts/file.txt"));
            Assert.NotNull(results);


        }

    }
}
