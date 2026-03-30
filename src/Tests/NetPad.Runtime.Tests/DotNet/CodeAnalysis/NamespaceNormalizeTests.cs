using NetPad.DotNet.CodeAnalysis;

namespace NetPad.Runtime.Tests.DotNet.CodeAnalysis;

public class NamespaceNormalizeTests
{
    [Theory]
    [InlineData("System.Text")]
    [InlineData("MyApp")]
    [InlineData("_Internal")]
    public void Normalize_AcceptsValidValues(string value)
    {
        var result = Namespace.Normalize(value);

        Assert.Equal(value, result);
    }

    [Fact]
    public void Normalize_TrimsWhitespace()
    {
        var result = Namespace.Normalize("  System.Text  ");

        Assert.Equal("System.Text", result);
    }

    [Fact]
    public void Normalize_RemovesLineEndings()
    {
        var result = Namespace.Normalize("System.Text\r\n.Json");

        Assert.Equal("System.Text.Json", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_ThrowsForNullOrWhitespace(string? value)
    {
        Assert.Throws<ArgumentException>(() => Namespace.Normalize(value!));
    }

    [Fact]
    public void Normalize_ThrowsWhenStartsWithNamespaceKeyword()
    {
        Assert.Throws<ArgumentException>(() => Namespace.Normalize("namespace System"));
    }

    [Theory]
    [InlineData("1System")]
    [InlineData(".System")]
    [InlineData("-Bad")]
    public void Normalize_ThrowsForInvalidFirstCharacter(string value)
    {
        Assert.Throws<ArgumentException>(() => Namespace.Normalize(value));
    }

    [Fact]
    public void Normalize_ThrowsWhenEndsWithSemicolon()
    {
        Assert.Throws<ArgumentException>(() => Namespace.Normalize("System.Text;"));
    }
}
