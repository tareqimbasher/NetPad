using NetPad.DotNet.CodeAnalysis;

namespace NetPad.Runtime.Tests.DotNet.CodeAnalysis;

public class UsingNormalizeTests
{
    [Theory]
    [InlineData("System.Text")]
    [InlineData("MyApp")]
    [InlineData("_Internal")]
    [InlineData("enc = System.Text.Encoding")]
    public void Normalize_AcceptsValidValues(string value)
    {
        var result = Using.Normalize(value);

        Assert.Equal(value, result);
    }

    [Fact]
    public void Normalize_TrimsWhitespace()
    {
        var result = Using.Normalize("  System.Text  ");

        Assert.Equal("System.Text", result);
    }

    [Fact]
    public void Normalize_RemovesLineEndings()
    {
        var result = Using.Normalize("System.Text\r\n.Json");

        Assert.Equal("System.Text.Json", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_ThrowsForNullOrWhitespace(string? value)
    {
        Assert.Throws<ArgumentException>(() => Using.Normalize(value!));
    }

    [Fact]
    public void Normalize_ThrowsWhenStartsWithUsingKeyword()
    {
        Assert.Throws<ArgumentException>(() => Using.Normalize("using System"));
    }

    [Theory]
    [InlineData("1System")]
    [InlineData(".System")]
    [InlineData("-Bad")]
    public void Normalize_ThrowsForInvalidFirstCharacter(string value)
    {
        Assert.Throws<ArgumentException>(() => Using.Normalize(value));
    }

    [Fact]
    public void Normalize_ThrowsWhenEndsWithSemicolon()
    {
        Assert.Throws<ArgumentException>(() => Using.Normalize("System.Text;"));
    }
}
