using Xunit;

namespace nc.Hub.Tests;

public class SafeStringFacts
{
	[Theory]
	[InlineData("Schema.Table", "Schema.Table")]
	[InlineData("[Schema].[Table]", "_Schema_._Table_")]
	public void StripsInvalidCharacters(string input, string expected)
	{
		Assert.Equal(expected, new SafeString(input).Value);
	}

	[Fact]
	public void UsesReplacementString()
	{
		var input = "[Schema].[Table]";
		var replacement = "";
		var sanitized = new SafeString(input, replacement);
		Assert.Equal("Schema.Table", sanitized.Value);
	}

	[Theory]
	[InlineData("Schema.Table'; DROP TABLE sys.tables", "Schema.Table___DROP_TABLE_sys.tables")]
	public void PreventsSqlInjection(string input, string expected)
	{
		Assert.Equal(expected, new SafeString(input).Value);
	}


}
