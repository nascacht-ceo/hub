using Amazon.DynamoDBv2.DataModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;

namespace nc.Aws;

public class SecretsStoreOptions
{
	public ParallelOptions ParallelOptions { get; set; }

	public JsonSerializerOptions JsonOptions { get; set; }

	public SecretsStoreOptions()
	{
		ParallelOptions = new ParallelOptions
		{
			MaxDegreeOfParallelism = Environment.ProcessorCount
		};
		JsonOptions = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = false
		};
	}

	public ParallelOptions GetParallelOptions(CancellationToken cancellationToken = default)
	{
		return new ParallelOptions
		{
			MaxDegreeOfParallelism = ParallelOptions.MaxDegreeOfParallelism,
			TaskScheduler = ParallelOptions.TaskScheduler,
			CancellationToken = cancellationToken
		};
	}

}

public class SecretsStoreOptions<T, TKey> : SecretsStoreOptions
{
	public PropertyInfo ModelKey { get; set; }

	public SecretsStoreOptions(): base()
	{
		var type = typeof(T);
		var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

		foreach (var prop in properties)
		{
			if (prop.GetCustomAttribute<KeyAttribute>() != null)
				ModelKey = prop;
			else if (prop.GetCustomAttribute<DynamoDBHashKeyAttribute>() != null)
				ModelKey = prop;
			else if (prop.GetCustomAttribute<DynamoDBRangeKeyAttribute>() != null)
				ModelKey = prop;
		}

		// Fallback: use first property as hash key if not found
		if (ModelKey == null && properties.Length > 0)
			ModelKey = properties.FirstOrDefault(p => p.PropertyType == typeof(string));

		if (ModelKey == null)
			throw new InvalidOperationException($"No suitable key property found for type {type.Name}. Consider marking a property with a System.ComponentModel.DataAnnotations.KeyAttribute.");
	}
}
