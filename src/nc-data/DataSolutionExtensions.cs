using nc.Hub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Data;

public static class DataSolutionExtensions
{
	public static Solution AddDatabase(this Solution solution, SafeString name, string connectionString)
	{
		if (solution.Endpoints.ContainsKey(name))
			throw new ArgumentOutOfRangeException(nameof(name), name, $"An endpoint with the name '{name}' already exists.");
		solution.Endpoints.Add(name, new SqlEndpoint() 
		{
			ConnectionString = connectionString 
		});
		return solution;
	}

	//public static async Task<DatabaseEndpoint> GetDatabaseModules(this DatabaseEndpoint databaseEndpoint, IEnumerable<SafeString> tableNames)
	//{
	//	if (databaseEndpoint == null) throw new ArgumentNullException(nameof(databaseEndpoint));
	//	if (tableNames == null) throw new ArgumentNullException(nameof(tableNames));

	//	foreach (var tableName in tableNames)
	//	{
	//		var module = new ModuleDefinition(tableName);
	//		databaseEndpoint.Modules.Add(module);
	//	}
	//	return databaseEndpoint;
	//}

	public static async Task<DatabaseEndpoint> AddDbContextAsync(this DatabaseEndpoint databaseEndpoint, IEnumerable<ModelDefinition> models)
	{
		return databaseEndpoint;
	}

	public static Task<DatabaseEndpoint> AddDbContextAsync(this Solution solution, SafeString databaseName, IEnumerable<ModelDefinition> models)
		=> solution.GetEndpoint<DatabaseEndpoint>(databaseName).AddDbContextAsync(models);
}
