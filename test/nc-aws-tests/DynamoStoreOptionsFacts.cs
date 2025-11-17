namespace nc.Aws.Tests;

public class DynamoStoreOptionsFacts
{
	[Fact]
	public void GetTableName_Uses_DynamoDBTable_Attribute_If_Present()
	{
		// ARRANGE
		var options = new DynamoStoreOptions();
		// ACT
		var tableName = options.GetTableName<MockEntity1>();
		// ASSERT
		Assert.Equal("Foo", tableName);
	}

	[Fact]
	public void GetTableName_Uses_Table_Attribute_If_Present()
	{
		// ARRANGE
		var options = new DynamoStoreOptions();
		// ACT
		var tableName = options.GetTableName<MockEntity2>();
		// ASSERT
		Assert.Equal("Bar", tableName);
	}


	[Amazon.DynamoDBv2.DataModel.DynamoDBTable("Foo")]
	public class MockEntity1
	{
		public string Id { get; set; } = string.Empty;
	}

	[System.ComponentModel.DataAnnotations.Schema.Table("Bar")]
	public class MockEntity2
	{
			public string Id { get; set; } = string.Empty;
	}
}
