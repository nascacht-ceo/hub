using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Microsoft.Extensions.DependencyInjection;
using nc.Cloud;

namespace nc.Azure.Tests;

[Collection(nameof(AzureFixture))]
public class ArmClientTests: IAsyncLifetime
{
	private readonly AzureFixture _fixture;
	private ArmClient? _client;

	public ArmClientTests(AzureFixture fixture)
	{
		_fixture = fixture;
	}

	public Task DisposeAsync()
	{
		return Task.CompletedTask;
	}

	public Task InitializeAsync()
	{

		_client = new AzureTenant().GetService<ArmClient>();
		return Task.CompletedTask;
	}

	[Fact(Skip = "work in progress")]
	public async Task ListsSubscriptions()
	{
		var subscriptions = _client!.GetSubscriptions();
		Assert.NotNull(subscriptions);
		Assert.NotEmpty(subscriptions.ToList());
		foreach (var subscription in subscriptions)
		{
			var accounts = subscription.GetStorageAccounts();
			Assert.NotNull(accounts);
			Assert.NotEmpty(accounts.ToList());
		}
	}

	[Fact]
	public async Task GetsTenant()
	{
		var tenants = _client!.GetTenants(); // {c16d0843-cfec-424a-8fd2-3ff0da3bfa58}
		Assert.NotNull(tenants);
		Assert.NotEmpty(tenants.ToList());
	}
}
