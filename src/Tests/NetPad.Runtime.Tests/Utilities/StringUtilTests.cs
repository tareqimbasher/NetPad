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
    public void EqualsIgnoreCase()
    {
        var first = "Hello";
        var second = "hello";

        Assert.True(StringUtil.EqualsIgnoreCase(first, second));
    }

    [Fact]
    public void ContainsIgnoreCase()
    {
        var first = "Hello World";
        var second = "o wor";

        Assert.True(StringUtil.ContainsIgnoreCase(first, second));
    }

    [Fact]
    public void EndsWithIgnoreCase()
    {
        var first = "Hello World";
        var second = "o world";

        Assert.True(StringUtil.EndsWithIgnoreCase(first, second));
    }

    [Theory]
    [InlineData("Hello World", 3, false, "Hel")]
    [InlineData("Hello World", 3, true, "Hel...")]
    [InlineData("Hello World", 0, false, "")]
    [InlineData("Hello World", 0, true, "...")]
    [InlineData("Hello World", 20, false, "Hello World")]
    [InlineData("Hello World", 20, true, "Hello World")]
    public void Truncate(string str,  int maxLength, bool withTrailingDots, string expected)
    {
        var result = StringUtil.Truncate(str, maxLength, withTrailingDots);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("hello", "", "hello")]
    [InlineData("hello", "world", "hello")]
    [InlineData("", "world", "world")]
    [InlineData(null, "world", "world")]
    public void DefaultIfNullOrWhitespace(string? str,  string defaultString, string expected)
    {
        var result = StringUtil.DefaultIfNullOrWhitespace(str, defaultString);

        Assert.Equal(expected, result);
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
    [InlineData("some words", "not exists", "ds", true, "some words")]
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

    [Theory]
    [InlineData("https://github.com", true)]
    [InlineData("relative/path", true)]
    public void ToUriOrDefault(string uriStr,  bool expectedSuccess)
    {
        var uri = StringUtil.ToUriOrDefault(uriStr);

        if (expectedSuccess) Assert.NotNull(uri);
        else Assert.Null(uri);
    }

    [Fact]
    public void SplitLastOccurence_WhenSearchStringExists()
    {
        var str = "one. two. three";

        var parts = StringUtil.SplitLastOccurence(str, ".");

        Assert.Equal(2, parts.Length);
        Assert.Equal(parts, ["one. two", " three"]);
    }

    [Fact]
    public void SplitLastOccurence_WhenSearchStringDoesNotExist()
    {
        var str = "one. two. three";

        var parts = StringUtil.SplitLastOccurence(str, "&");

        Assert.Single(parts);
        Assert.Equal(parts, ["one. two. three"]);
    }

    [Fact]
    public void RemoveInvalidFileNameCharacters()
    {
        var invalidFileNameChar = Path.GetInvalidFileNameChars()[0];
        var name = $"File{invalidFileNameChar}name.txt";

        var result = StringUtil.RemoveInvalidFileNameCharacters(name);

        Assert.Equal("Filename.txt", result);
    }
}
