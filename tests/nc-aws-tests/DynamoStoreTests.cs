using Microsoft.Extensions.DependencyInjection;
using nc.Cloud;
using nc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Aws.Tests;

[Collection((nameof(AmazonFixture)))]
public class DynamoStoreTests
{
	private readonly AmazonFixture _fixture;

	public DynamoStoreTests(AmazonFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task PersistsData()
	{
		var id = Guid.NewGuid().ToString();
		var entity = new MockEntity
		{
			Id = id,
			Name = "PersistsData"
		};
		var store = _fixture.Services.GetRequiredService<IStore<MockEntity, string>>();
		Assert.NotNull(store);
		await store.PostAsync(entity);
		var retrieved = await store.GetAsync(id);
		Assert.NotNull(retrieved);
		Assert.Equal(entity.Name, retrieved.Name);
	}

	[Fact]
	public async Task DeletesData()
	{
		var id = Guid.NewGuid().ToString();
		var entity = new MockEntity
		{
			Id = id,
			Name = "DeletesData"
		};
		var store = _fixture.Services.GetRequiredService<IStore<MockEntity, string>>();
		Assert.NotNull(store);
		await store.PostAsync(entity);
		var retrieved = await store.GetAsync(id);
		Assert.NotNull(retrieved);
		Assert.Equal(entity.Name, retrieved.Name);

		await store.DeleteAsync(id);
		var deleted = await store.GetAsync(id);
		Assert.Null(deleted);
	}

	public class MockEntity
	{
		public required string Id { get; set; }
		public string? Name { get; set; }
	}
}
