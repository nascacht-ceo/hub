using Microsoft.Data.SqlClient;
using nc.Hub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace nc.Data;

public interface DatabaseEndpoint : IEndpoint
{
	/// <summary>
	/// Gets or sets the connection string used to establish a connection to the database.
	/// </summary>
	/// <remarks>Ensure the connection string is properly formatted and includes all required parameters.  Invalid
	/// or incomplete connection strings may result in connection failures.</remarks>
	public string ConnectionString { get; set; }

	/// <summary>
	/// Asynchronously retrieves a collection of model definitions based on the specified schema and table filters.
	/// </summary>
	/// <remarks>This method supports asynchronous streaming of results, allowing the caller to process each  <see
	/// cref="ModelDefinition"/> as it becomes available. The filters are applied using SQL-style  pattern matching, where
	/// "%" matches zero or more characters and "_" matches a single character.</remarks>
	/// <param name="schemaFilter">A filter for the schema names to include in the results. The default value is <see langword="%" />,  which matches
	/// all schemas. Use SQL-style wildcards (e.g., "%" or "_") for pattern matching.</param>
	/// <param name="tableFilter">A filter for the table names to include in the results. The default value is <see langword="%" />,  which matches
	/// all tables. Use SQL-style wildcards (e.g., "%" or "_") for pattern matching.</param>
	/// <param name="cancellation">A <see cref="CancellationToken"/> to observe while waiting for the asynchronous operation to complete.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> of <see cref="ModelDefinition"/> objects that match the specified filters.</returns>
	public IAsyncEnumerable<ModelDefinition> GetModelsAsync(string? schemaFilter = "%", string? tableFilter = "%", CancellationToken cancellation = default);
}

public class SqlEndpoint: DatabaseEndpoint
{
	private const string _query = @"
SELECT 
    c.TABLE_SCHEMA,
    c.TABLE_NAME,
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.IS_NULLABLE,
    CASE 
        WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 
        ELSE 0 
    END AS IS_PRIMARY_KEY,
    fk.REFERENCED_TABLE_SCHEMA,
    fk.REFERENCED_TABLE_NAME,
    fk.REFERENCED_COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS c
JOIN INFORMATION_SCHEMA.TABLES t
    ON c.TABLE_SCHEMA = t.TABLE_SCHEMA
    AND c.TABLE_NAME = t.TABLE_NAME
    AND t.TABLE_TYPE = 'BASE TABLE' 
LEFT JOIN (
    SELECT kcu.TABLE_SCHEMA, kcu.TABLE_NAME, kcu.COLUMN_NAME
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu 
      ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
) pk 
    ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA 
    AND c.TABLE_NAME = pk.TABLE_NAME 
    AND c.COLUMN_NAME = pk.COLUMN_NAME
LEFT JOIN (
    SELECT 
        kcu.TABLE_SCHEMA, 
        kcu.TABLE_NAME, 
        kcu.COLUMN_NAME,
        ccu.TABLE_SCHEMA AS REFERENCED_TABLE_SCHEMA,
        ccu.TABLE_NAME AS REFERENCED_TABLE_NAME,
        ccu.COLUMN_NAME AS REFERENCED_COLUMN_NAME
    FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu 
      ON rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
    JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu 
      ON rc.UNIQUE_CONSTRAINT_NAME = ccu.CONSTRAINT_NAME
) fk 
    ON c.TABLE_SCHEMA = fk.TABLE_SCHEMA 
    AND c.TABLE_NAME = fk.TABLE_NAME 
    AND c.COLUMN_NAME = fk.COLUMN_NAME
WHERE c.TABLE_SCHEMA LIKE @SchemaFilter
  AND c.TABLE_NAME LIKE @TableFilter
ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION;
";

	public required string ConnectionString { get; set; }

	private static Type MapSqlType(string sqlType, bool isNullable) =>
	sqlType.ToLowerInvariant() switch
	{
		"int" => isNullable ? typeof(int?) : typeof(int),
		"bigint" => isNullable ? typeof(long?) : typeof(long),
		"smallint" => isNullable ? typeof(short?) : typeof(short),
		"tinyint" => isNullable ? typeof(byte?) : typeof(byte),
		"bit" => isNullable ? typeof(bool?) : typeof(bool),
		"decimal" or "numeric" => isNullable ? typeof(decimal?) : typeof(decimal),
		"float" => isNullable ? typeof(double?) : typeof(double),
		"real" => isNullable ? typeof(float?) : typeof(float),
		"datetime" or "smalldatetime" or "datetime2" => isNullable ? typeof(DateTime?) : typeof(DateTime),
		"datetimeoffset" => isNullable ? typeof(DateTimeOffset?) : typeof(DateTimeOffset),
		"date" => isNullable ? typeof(DateTime?) : typeof(DateTime),
		"time" => isNullable ? typeof(TimeSpan?) : typeof(TimeSpan),
		"char" or "nchar" or "varchar" or "nvarchar" or "text" or "ntext" => typeof(string),
		"uniqueidentifier" => isNullable ? typeof(Guid?) : typeof(Guid),
		"binary" or "varbinary" => typeof(byte[]),
		"geography" => typeof(NetTopologySuite.Geometries.Point),
		"xml" => typeof(System.Xml.Linq.XDocument),
		_ => typeof(object)
	};


	public async IAsyncEnumerable<ModelDefinition> GetModelsAsync(string? schemaFilter = "%", string? tableFilter = "%", [EnumeratorCancellation] CancellationToken cancellation = default)
	{
		using var connection = new SqlConnection(ConnectionString);
		connection.Open();
		var cmd = connection.CreateCommand();
		cmd.CommandText = _query;
		cmd.Parameters.AddWithValue("@SchemaFilter", schemaFilter);
		cmd.Parameters.AddWithValue("@TableFilter", tableFilter);


		var tableMap = new Dictionary<(string Schema, string Table), List<(PropertyDefinition Property, string? RefClass)>>();

		using var reader = await cmd.ExecuteReaderAsync();
		while (await reader.ReadAsync())
		{
			string schema = reader.GetString(0);
			string table = reader.GetString(1);
			string column = reader.GetString(2);
			string sqlType = reader.GetString(3);
			bool isNullable = reader.GetString(4) == "YES";
			bool isKey = reader.GetInt32(5) == 1;

			string? refSchema = reader.IsDBNull(6) ? null : reader.GetString(6);
			string? refTable = reader.IsDBNull(7) ? null : reader.GetString(7);
			string? refColumn = reader.IsDBNull(8) ? null : reader.GetString(8);

			string modelName = $"{schema}_{table}";
			string? referencedModelName = refSchema != null && refTable != null
				? $"{refSchema}_{refTable}"
				: null;

			var property = new PropertyDefinition
			{
				Name = column,
				ClrType = MapSqlType(sqlType, isNullable),
				IsKey = isKey,
				IsStatus = false,
				DeclaringType = null,
				Description = null
			};

			var key = (schema, table);
			if (!tableMap.TryGetValue(key, out var list))
				tableMap[key] = list = new();

			list.Add((property, referencedModelName));
		}

		var results = new List<ModelDefinition>();

		foreach (var ((schema, table), propList) in tableMap)
		{
			var modelName = $"{schema}_{table}";
			var properties = new List<PropertyDefinition>();

			foreach (var (prop, referencedClassName) in propList)
			{
				properties.Add(prop);

				//if (referencedClassName != null)
				//{
				//    // Add the navigation property
				//    var navProp = new PropertyDefinition
				//    {
				//        Name = referencedClassName, // You may want to make this prettier
				//        ClrType = Type.GetType($"{solutionNamespace}.{referencedClassName}") ?? typeof(object),
				//        IsKey = false,
				//        IsStatus = false,
				//        DeclaringType = null,
				//        Description = null
				//    };

				//    // Avoid duplication
				//    if (!properties.Any(p => p.Name == navProp.Name))
				//        properties.Add(navProp);
				//}
			}
			yield return new ModelDefinition
			{
				ModelName = modelName,
				Properties = properties,
				Interfaces = new HashSet<Type>(), // Add default interfaces if needed
				BaseClass = null,
				SourceType = "DatabaseTable",
				SourceUri = ConnectionString
			};
		}
	}

}
