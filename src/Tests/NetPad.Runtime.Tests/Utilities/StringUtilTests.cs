using System.Text;
using NetPad.Utilities;
using Xunit;

namespace NetPad.Runtime.Tests.Utilities;

public class StringUtilTests
{
    [Fact]
    public void JoinToString_CreatesAStringFromACollection()
    {
        var collection = new[] { "Test1", "Test2" };

        var joinedStr = collection.JoinToString(",");

        Assert.Equal("Test1,Test2", joinedStr);
    }

    [Fact]
    public void RemoveLeadingBOMString_RemovesLeadingBOMString()
    {
        var strWithBOM = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble()) + "test";

        var strWithoutBOM = StringUtil.RemoveLeadingBOMString(strWithBOM);

        Assert.NotEqual("test", strWithBOM);
        Assert.Equal("test", strWithoutBOM);
    }

    [Fact]
    public void RemoveLeadingBOMString_DoesNotRemoveNonLeadingBOMString()
    {
        var strWithBOM = "test" + Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());

        var result = StringUtil.RemoveLeadingBOMString(strWithBOM);

        Assert.Equal(strWithBOM, result);
    }

    [Theory]
    [InlineData("<foo>", "<", ">", false, "foo")]
    [InlineData("bar<foo>bar", "<", ">", false, "foo")]
    [InlineData("bar <foo> bar", "<", ">", false, "foo")]
    [InlineData("bar <foo>bar", "<", ">", false, "foo")]
    [InlineData("bar <foo>bar", "<", ">", true, "foo")]
    [InlineData("Some <foo>> words", "<", ">", false, "foo")]
    [InlineData("Some <foo>> words", "<", ">", true, "foo>")]
    [InlineData("Some <<foo>> words", "<", ">", false, "<foo")]
    [InlineData("Some <<foo>> words", "<", ">", true, "<foo>")]
    [InlineData("Somefoobars", "Some", "bar", false, "foo")]
    [InlineData("Somefoobarbars", "Some", "bar", true, "foobar")]
    public void SubstringBetween_ExtractsSubstringCorrectly(
        string str,
        string startDelimiter,
        string endDelimiter,
        bool useLastEndDelimiterOccurrence,
        string expectedResult)
    {
        var result = StringUtil.SubstringBetween(
            str,
            startDelimiter,
            endDelimiter,
            useLastEndDelimiterOccurrence);

        Assert.Equal(expectedResult, result);
    }
}
