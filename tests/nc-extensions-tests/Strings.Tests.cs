namespace nc.Extensions.Tests
{
    public class IsWildCardMatch
    {
        [Theory]
        [InlineData("Foo", "F*", true)]
        [InlineData("Foo", "*o", true)]
        [InlineData("Foo", "F*o", true)]
        [InlineData("Foo", "B*r", false)]
        public void MatchesAsterisk(string input, string match, bool expected)
        {
            Assert.Equal(expected, input.IsWildcardMatch(match));
        }

        [Theory]
        [InlineData("Foo", "F?o", true)]
        [InlineData("Foo", "F?", false)]
        [InlineData("Foo", "?o", false)]
        [InlineData("Foo", "B?r", false)]
        public void MatchesQuestionMark(string input, string match, bool expected)
        {
            Assert.Equal(expected, input.IsWildcardMatch(match));
        }


        [Theory]
        [InlineData("Foo", "Foo", true)]
        [InlineData("Foo", "Bar", false)]
        [InlineData("Foo", "F", false)]
        [InlineData("Foo", "FooBar", false)]
        public void MatchesFullStrings(string input, string match, bool expected)
        {
            Assert.Equal(expected, input.IsWildcardMatch(match));
        }


        [Fact]
        public void DoesNotMatchNull()
        {
            Assert.False("Foo".IsWildcardMatch(null!));
        }
    }
}
