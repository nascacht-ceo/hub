using nc.Cloud.Tests;

[Collection(nameof(AzureFixture))]
public class CloudFileManagerTests: AbstractCloudFileManagerTests
{
    public CloudFileManagerTests(AzureFixture fixture): base(fixture)
    { }

}

