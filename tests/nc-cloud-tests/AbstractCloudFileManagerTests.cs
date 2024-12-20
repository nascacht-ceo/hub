public abstract class AbstractCloudFileManagerTests
{
    ITestFixture Fixture { get; set; }

    public AbstractCloudFileManagerTests(ITestFixture fixture)
    {
        Fixture = fixture;
    }


    [Fact]
    public void EnumeratesServices()
    {
        // Assert
        Assert.NotNull(Fixture.Manager.Keys);
    }

    [Fact]
    public void InstantiatesServices()
    {
        foreach (var service in Fixture.Manager.Keys)
        {
            // Act
            var manager = Fixture.Manager[service];
            Assert.NotNull(manager);
        }
    }

    [Fact]
    public async Task EnumeratesProviders()
    {
        var service = Fixture.Manager.Keys.First();
        Assert.NotNull(service);
        var first = Fixture.Manager[service];
        var providers = await first.SearchAsync("*").ToListAsync();
        Assert.NotNull(providers);
    }


    [Fact]
    public async Task AddsProvider()
    {
        var name = $"{Guid.NewGuid()}";
        var service = Fixture.Manager[Fixture.Manager.Keys.First()];
        var provider = await service.CreateAsync(name);
        var providers = await service.SearchAsync("*").ToListAsync();
        Assert.Contains(name, providers.Select(p => p.Name));
        await service.DeleteAsync(name);
    }

}
