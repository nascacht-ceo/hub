using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using nc.Hub;
using nc.Reflection;
using NetTopologySuite.Geometries;
using System.Xml.Linq;

namespace nc.Data.Tests;

public class Sample : IAsyncLifetime
{
	private readonly TypeService _typeService;
	private static readonly string ConnectionString = "Server=(localdb)\\mssqllocaldb;Database=AdventureWorks;Trusted_Connection=True;";

	public Sample()
	{
		_typeService = new TypeService();
	}

	public async Task InitializeAsync()
	{
		var sqlReflection = new SqlReflection(ConnectionString);
		await foreach (var modelDefinition in sqlReflection.DiscoverModels("AdventureWorks", "Person", "%"))
		{
			var type = _typeService.GetModel(modelDefinition);
			Assert.NotNull(type);
		}
	}

	public Task DisposeAsync()
	{
		return Task.CompletedTask;
	}

	[Fact(Skip = "work in progress")]
	public async Task QueriesPocoTypes()
	{
		var options = new DbContextOptionsBuilder<DynamicDbContext>()
			.UseInMemoryDatabase("DynamicDbTest")
			.Options;

		var sampleDefinition = new ModelDefinition<SampleClass>();
		sampleDefinition.Properties.First(p => p.Name == "Id").IsKey = true;
		var sampleType = _typeService.GetModel(sampleDefinition);

		// Act
		using (var context = new DynamicDbContext(options, _typeService, [sampleDefinition]))
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
		using (var context = new DynamicDbContext(options, _typeService, [sampleDefinition]))
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
		Assert.NotEmpty(_typeService.GetTypes());
	}

	[Fact(Skip = "work in progress")]
	public async Task QueriesTables()
	{
		var options = new DbContextOptionsBuilder<DynamicDbContext>()
			.UseSqlServer(ConnectionString, sql => sql.UseNetTopologySuite())
			.Options;



		var types = _typeService.GetTypes("AdventureWorks").ToList();
		var addressType = types.First(t => t.Name == "Person_Address");
		using var context = new DynamicDbContext(options, _typeService, _typeService.GetModelDefinitions("AdventureWorks"));

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
