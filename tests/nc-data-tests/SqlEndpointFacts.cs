using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Data.Tests;

[Collection(nameof(Fixture))]
public class SqlEndpointFacts
{
	private readonly Fixture _fixture;
	private readonly SqlEndpoint _sqlEndpoint;

	public SqlEndpointFacts(Fixture fixture)
	{
		_fixture = fixture;

		_sqlEndpoint = new SqlEndpoint() { ConnectionString = _fixture.ConnectionString };
	}

	public class GetModelsAsync: SqlEndpointFacts
	{
		public GetModelsAsync(Fixture fixture) : base(fixture)
		{
		}

		[Fact]
		public async Task FiltersBySchema()
		{
			await foreach (var model in _sqlEndpoint.GetModelsAsync("Person", "%"))
			{
				Assert.StartsWith("Person", model.ModelName);
			}
		}

		[Fact]
		public async Task FiltersbyTable()
		{
			await foreach (var model in _sqlEndpoint.GetModelsAsync("%", "Sales"))
			{
				Assert.Contains(".Sales", model.ModelName);
			}

		}

		[Fact]
		public async Task CreatesModelDefinition()
		{
			var model = await _sqlEndpoint.GetModelsAsync("Person", "Address").FirstOrDefaultAsync();
			Assert.NotNull(model);
			Assert.NotEmpty(model.Properties);
			Assert.Contains(model.Properties, p => p.Name == "AddressID" && p.ClrType == typeof(int));
		}

	}
}
