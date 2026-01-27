using DotNet.Testcontainers.Builders;
using Microsoft.Data.SqlClient;
using nc.Reflection;
using Testcontainers.MsSql;

namespace nc.Data.Tests;

public class Fixture : IAsyncLifetime
{
	public Fixture()
	{
		SqlContainer = new MsSqlBuilder("chriseaton/adventureworks:latest")
			.WithPassword("nc_Test_Pipeline1!")
			.WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("(/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'nc_Test_Pipeline1!' -C -Q 'SELECT name FROM sys.databases' | grep AdventureWorks)"))
			.Build();
		TypeService = new TypeService();
	}

	public MsSqlContainer SqlContainer { get; }
	public TypeService TypeService { get; }
	public string? ConnectionString { get; private set; }

	public Task DisposeAsync()
	{
		return SqlContainer.StopAsync();
	}

	public async Task InitializeAsync()
	{
		await SqlContainer.StartAsync();
		ConnectionString = new SqlConnectionStringBuilder(SqlContainer.GetConnectionString())
		{
			InitialCatalog = "AdventureWorks",
			TrustServerCertificate = true
		}.ConnectionString;

		var sqlReflection = new SqlReflection(SqlContainer.GetConnectionString());
		await foreach (var modelDefinition in sqlReflection.DiscoverModels("AdventureWorks", "Person", "%"))
		{
			var type = TypeService.GetModel(modelDefinition);
			Assert.NotNull(type);
		}
	}
}

[CollectionDefinition(nameof(Fixture))]
public class FixtureCollection : ICollectionFixture<Fixture> { }