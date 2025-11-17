using Amazon.CDK;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace nc.Aws;

/// <summary>
/// Represents configuration options for interacting with a DynamoDB table, including table settings, batch operation
/// configurations, and logging support.
/// </summary>
/// <remarks>This class provides a set of properties and methods to configure and manage interactions with a
/// DynamoDB table. It includes options for table creation, batch operations, and logging. The default values for
/// properties are chosen to provide a balance between usability and performance, but they can be customized to suit
/// specific application requirements.</remarks>
public class DynamoStoreOptions : DynamoDBContextConfig
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DynamoStoreOptions"/> class.
	/// </summary>
	public DynamoStoreOptions() : base()
    {
    }

	/// <summary>
	/// Gets or sets the name of the database table associated with this entity.
	/// </summary>
	public string? TableName { get; set; }

	/// <summary>
	/// Timeout to use for table creation.
	/// Defaults to 30 seconds.
	/// </summary>
	public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

	/// <summary>
	/// Gets or sets the delay duration while polling for activation.
	/// Defaults to 1 second.
	/// </summary>
	public TimeSpan ActivateDelay { get; set; } = TimeSpan.FromSeconds(1);

	/// <summary>
	/// Gets or sets the billing mode for the database.
	/// Defaults to <see cref="BillingMode.PAY_PER_REQUEST"/>.
	/// </summary>
	/// <remarks>Use <see cref="BillingMode.PAY_PER_REQUEST"/> for on-demand billing based on usage,  or <see
	/// cref="BillingMode.PROVISIONED"/> for a fixed capacity model.</remarks>
	public BillingMode BillingMode { get; set; } = BillingMode.PAY_PER_REQUEST;

	/// <summary>
	/// Tags to apply to the created table.
	/// </summary>
	public List<Amazon.DynamoDBv2.Model.Tag> Tags { get; set; } = new List<Amazon.DynamoDBv2.Model.Tag>();

	//public SaveConfig SaveConfig { get; set; }
	//public DeleteConfig DeleteConfig { get; set; }
	//public LoadConfig LoadConfig { get; set; }

	/// <summary>
	/// Gets or sets the configuration settings for batch retrieval operations.
	/// If not set, a default instance of <see cref="BatchGetConfig"/> will be created using the current <see cref="TableName"/>.
	/// </summary>
	public BatchGetConfig? BatchGetConfig { get; set; }

	/// <summary>
	/// Gets the configuration settings for batch write operations.
	/// If not set, a default instance of <see cref="BatchWriteConfig"/> will be created using the current <see cref="TableName"/>.
	/// </summary>
	/// <remarks>This property is typically used to configure or inspect the settings that control how batch  write
	/// operations are performed. The settings may include parameters such as the maximum  number of items per batch or the
	/// retry strategy for failed writes.</remarks>
	public BatchWriteConfig? BatchWriteConfig { get; internal set; }

	/// <summary>
	/// Gets or sets the batch size used for read operations.
	/// Defaults to 100.
	/// </summary>
	public int BatchSizeGet { get; set; } = 100;

	/// <summary>
	/// Gets or sets the number of items to process in a single batch during write operations.
	/// Defaults to 25.
	/// </summary>
	/// <remarks>Adjusting this value can impact performance. A larger batch size may improve throughput  for bulk
	/// operations but could increase memory usage. A smaller batch size may reduce memory  usage but could result in more
	/// frequent write operations.</remarks>
	public int BatchSizeWrite { get; set; } = 100;

	/// <summary>
	/// Name of the tenant associated with this store configuration.
	/// If null, implicit credentials will be used.
	/// </summary>
	public string? TenantName { get; set; }

	/// <summary>
	/// Asynchronously retrieves a configured <see cref="IDynamoDBContext"/> instance for interacting with Amazon DynamoDB.
	/// </summary>
	/// <remarks>This method ensures that the DynamoDB table for the specified entity type <typeparamref name="T"/>
	/// exists before returning the context. The returned context is configured with various settings, such as table name
	/// prefix,  null value handling, and metadata caching mode, based on the current configuration of the class.</remarks>
	/// <typeparam name="T">The type representing the DynamoDB table entity. This is used to determine the table name and schema.</typeparam>
	/// <param name="client">The <see cref="IAmazonDynamoDB"/> client used to interact with the DynamoDB service.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the asynchronous operation.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="IDynamoDBContext"/>
	/// instance configured with the specified client and context settings.</returns>
	public async Task<IDynamoDBContext> GetContextAsync<T>(IAmazonDynamoDB client, Microsoft.Extensions.Logging.ILogger? logger = null, CancellationToken cancellationToken = default)
	{
		TableName ??= GetTableName<T>();
		await EnsureTableAsync<T>(client, logger, cancellationToken);
		return new DynamoDBContextBuilder()
			.WithDynamoDBClient(() => client)
			.ConfigureContext(config =>
			{
				config.TableNamePrefix = TableNamePrefix;
				config.IgnoreNullValues	= IgnoreNullValues;
				config.SkipVersionCheck = SkipVersionCheck;
				config.Conversion = Conversion;
				config.DisableFetchingTableMetadata = DisableFetchingTableMetadata;
				config.ConsistentRead = ConsistentRead;
				config.DerivedTypeAttributeName = DerivedTypeAttributeName;
				config.IsEmptyStringValueEnabled = IsEmptyStringValueEnabled;
				config.MetadataCachingMode = MetadataCachingMode;
				config.RetrieveDateTimeInUtc = RetrieveDateTimeInUtc;
			})
			.Build();
	}

	/// <summary>
	/// Retrieves the table name for the specified type, using a combination of attributes and conventions.
	/// </summary>
	/// <remarks>The table name is prefixed with the value of the <c>TableNamePrefix</c> field or property, if
	/// defined. This method is commonly used in scenarios where table names are dynamically resolved based on type
	/// metadata.</remarks>
	/// <typeparam name="T">The type for which to determine the table name.</typeparam>
	/// <returns>A string representing the table name. If the type is decorated with a <see cref="DynamoDBTableAttribute"/>, the
	/// table name is derived from its <see cref="DynamoDBTableAttribute.TableName"/> property. If the type is decorated
	/// with a <see cref="System.ComponentModel.DataAnnotations.Schema.TableAttribute"/>, the table name is derived from
	/// its <see cref="System.ComponentModel.DataAnnotations.Schema.TableAttribute.Name"/> property. If neither attribute
	/// is present, the table name defaults to the type's name.</returns>
	public string GetTableName<T>()
    {
        var type = typeof(T);

        // 1. Check for [DynamoDBTable]
        var dynamoAttribute = type.GetCustomAttribute<DynamoDBTableAttribute>();
        if (dynamoAttribute != null && !string.IsNullOrWhiteSpace(dynamoAttribute.TableName))
            return $"{TableNamePrefix}{dynamoAttribute.TableName}";

        // 2. Check for [Table] (System.ComponentModel.DataAnnotations.Schema)
        var tableAttribute = type.GetCustomAttribute<TableAttribute>();
        if (tableAttribute != null && !string.IsNullOrWhiteSpace(tableAttribute.Name))
            return $"{TableNamePrefix}{tableAttribute.Name}";

        // 3. Fallback to type name
        return $"{TableNamePrefix}{type.Name}";
    }

	public async Task EnsureTableAsync<T>(IAmazonDynamoDB client, Microsoft.Extensions.Logging.ILogger? logger, CancellationToken cancellationToken = default)
	{
		TableName = GetTableName<T>();
		BatchGetConfig ??= new BatchGetConfig { OverrideTableName = TableName };
		BatchWriteConfig ??= new BatchWriteConfig { OverrideTableName = TableName };
		try
		{
			var response = await client.DescribeTableAsync(TableName);
			if (response.Table.TableStatus == TableStatus.ACTIVE)
				return;
		}
		catch (ResourceNotFoundException)
		{ 
			logger?.LogInformation("Table '{TableName}' does not exist.", TableName);
		}

		var type = typeof(T);
		var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

		PropertyInfo? hashKeyProp = null;
		PropertyInfo? rangeKeyProp = null;

		foreach (var prop in properties)
		{
			if (prop.GetCustomAttribute<DynamoDBHashKeyAttribute>() != null)
				hashKeyProp = prop;
			else if (prop.GetCustomAttribute<DynamoDBRangeKeyAttribute>() != null)
				rangeKeyProp = prop;
		}

		// Fallback: use first property as hash key if not found
		if (hashKeyProp == null && properties.Length > 0)
			hashKeyProp = properties[0];

		if (hashKeyProp == null)
			throw new InvalidOperationException($"No suitable key property found for type {type.Name}. Consider marking a property with a DynamoDBHashKeyAttribute or DynamoDBRangeKeyAttribute attribute.");

		var attributeDefinitions = new List<AttributeDefinition>();
		var keySchema = new List<KeySchemaElement>();

		// Partition key
		attributeDefinitions.Add(new AttributeDefinition
		{
			AttributeName = hashKeyProp.Name,
			AttributeType = GetScalarAttributeType(hashKeyProp.PropertyType)
		});
		keySchema.Add(new KeySchemaElement
		{
			AttributeName = hashKeyProp.Name,
			KeyType = KeyType.HASH
		});

		// Sort key (optional)
		if (rangeKeyProp != null)
		{
			attributeDefinitions.Add(new AttributeDefinition
			{
				AttributeName = rangeKeyProp.Name,
				AttributeType = GetScalarAttributeType(rangeKeyProp.PropertyType)
			});
			keySchema.Add(new KeySchemaElement
			{
				AttributeName = rangeKeyProp.Name,
				KeyType = KeyType.RANGE
			});
		}


		// Assumes partition key is "Id" of type string
		var request = new CreateTableRequest
		{
			TableName = TableName,
			AttributeDefinitions = attributeDefinitions,
			KeySchema = keySchema,
			BillingMode = BillingMode,
			Tags = Tags
		};
		logger?.LogInformation("Creating table '{TableName}'.", TableName);
		await client.CreateTableAsync(request, cancellationToken);

		while (!cancellationToken.IsCancellationRequested)
		{
			var response = await client.DescribeTableAsync(TableName);
			if (response.Table.TableStatus == TableStatus.ACTIVE)
				break;
			await Task.Delay(ActivateDelay);
		}
	}

	private static ScalarAttributeType GetScalarAttributeType(Type type)
	{
		if (type == typeof(string))
			return ScalarAttributeType.S;
		if (type == typeof(int) || type == typeof(long) || type == typeof(short) ||
			type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) ||
			type == typeof(byte) || type == typeof(sbyte) || type == typeof(decimal) ||
			type == typeof(float) || type == typeof(double))
			return ScalarAttributeType.N;
		if (type == typeof(byte[]))
			return ScalarAttributeType.B;
		throw new NotSupportedException($"Type '{type.Name}' is not supported as a DynamoDB key attribute.");
	}
}

/// <summary>
/// Provides configuration options for a DynamoDB-backed store for a specific entity type.
/// </summary>
/// <remarks>This class extends <see cref="DynamoStoreOptions"/> to allow configuration specific to the entity
/// type <typeparamref name="T"/>.</remarks>
/// <typeparam name="T">The type of the entity that the store will manage. Must be a reference type.</typeparam>
public class DynamoStoreOptions<T> : DynamoStoreOptions where T : class
{
	/// <summary>
	/// Provides configuration options for a DynamoDB-backed store.
	/// </summary>
	public DynamoStoreOptions() : base()
	{ }
}