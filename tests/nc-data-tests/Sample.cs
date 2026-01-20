using Microsoft.EntityFrameworkCore;
using nc.Hub;
using nc.Reflection;

namespace nc.Data.Tests;

[Collection(nameof(Fixture))]
public class Sample
{
	private readonly Fixture _fixture;
	private readonly string ConnectionString;

	public Sample(Fixture fixture)
	{
		_fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
		// _typeService = new TypeService();
		ConnectionString = _fixture.SqlContainer.GetConnectionString();
	}



	[Fact(Skip = "work in progress")]
	public async Task QueriesPocoTypes()
	{
		var options = new DbContextOptionsBuilder<DynamicDbContext>()
			.UseInMemoryDatabase("DynamicDbTest")
			.Options;

		var sampleDefinition = new ModelDefinition<SampleClass>();
		sampleDefinition.Properties.First(p => p.Name == "Id").IsKey = true;
		var sampleType = _fixture.TypeService.GetModel(sampleDefinition);

		// Act
		using (var context = new DynamicDbContext(options, _fixture.TypeService, [sampleDefinition]))
		{
			context.Database.EnsureCreated();


			var entity = Activator.CreateInstance(sampleType);
			Assert.NotNull(entity);
			sampleType.GetProperty("Id")!.SetValue(entity, 27);
			sampleType.GetProperty("Name")!.SetValue(entity, "Queries Poco Types");
			context.Add(entity);
			context.SaveChanges();
		}

		object? fetched;
		using (var context = new DynamicDbContext(options, _fixture.TypeService, [sampleDefinition]))
		{
			var set = (IQueryable)typeof(DbContext)
				.GetMethod("Set", Type.EmptyTypes)!
				.MakeGenericMethod(sampleType)
				.Invoke(context, null)!;

			fetched = set.Cast<IIdentity<long>>().FirstOrDefault(e => e.Id == 27);
		}

		// Assert
		Assert.NotNull(fetched);
		Assert.Equal(27, (long)sampleType.GetProperty("Id")!.GetValue(fetched)!);
		Assert.Equal("Queries Poco Types", sampleType.GetProperty("Name")!.GetValue(fetched));
	}

	[Fact(Skip = "work in progress")]
	public async Task DiscoversTables()
	{
		Assert.NotEmpty(_fixture.TypeService.GetTypes());
	}

	[Fact(Skip = "work in progress")]
	public async Task QueriesTables()
	{
		var options = new DbContextOptionsBuilder<DynamicDbContext>()
			.UseSqlServer(ConnectionString, sql => sql.UseNetTopologySuite())
			.Options;



		var types = _fixture.TypeService.GetTypes("AdventureWorks").ToList();
		var addressType = types.First(t => t.Name == "Person_Address");
		using var context = new DynamicDbContext(options, _fixture.TypeService, _fixture.TypeService.GetModelDefinitions("AdventureWorks"));

		var set = (IQueryable)typeof(DbContext)
			.GetMethod("Set", Type.EmptyTypes)!
			.MakeGenericMethod(addressType)
			.Invoke(context, null)!;
		var items = set.Cast<object>().Take(100).ToList();
		Assert.Equal(100, items.Count);
	}


	public class SampleClass : IIdentity<long>
	{
		public long Id { get; set; }
		public string? Name { get; set; }
	}

	public interface IIdentity<T>
	{
		T Id { get; set; }
	}
}
