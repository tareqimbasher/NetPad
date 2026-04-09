using NetPad.DotNet.CodeAnalysis;

namespace NetPad.Runtime.Tests.DotNet.CodeAnalysis;

public class CodeTests
{
    // --- Constructors ---

    [Fact]
    public void Constructor_StringOnly_SetsValueWithNullNamespace()
    {
        var code = new Code("test");

        Assert.Equal("test", code.Value);
        Assert.Null(code.Namespace);
    }

    [Fact]
    public void Constructor_WithNamespaceAndValue_SetsBoth()
    {
        var ns = new Namespace("MyApp");
        var code = new Code(ns, "Console.WriteLine();");

        Assert.Equal(ns, code.Namespace);
        Assert.Equal("Console.WriteLine();", code.Value);
    }

    // --- Equality ---

    [Fact]
    public void Equality_SameValue()
    {
        var code1 = new Code("Console.WriteLine();");
        var code2 = new Code("Console.WriteLine();");

        Assert.Equal(code1, code2);
    }

    [Fact]
    public void Equality_HashSet_Deduplicates()
    {
        var set = new HashSet<Code>();

        set.Add(new Code("Console.WriteLine();"));
        set.Add(new Code("Console.WriteLine();"));

        Assert.Single(set);
    }

    // --- ValueChanged ---

    [Fact]
    public void ValueChanged_FalseInitially()
    {
        var code = new Code("Console.WriteLine();");

        Assert.False(code.ValueChanged());
    }

    [Fact]
    public void ValueChanged_TrueAfterUpdate()
    {
        var code = new Code("Console.WriteLine();");

        code.Update("Console.ReadLine();");

        Assert.True(code.ValueChanged());
    }

    [Fact]
    public void ValueChanged_TrueWhenNamespaceChanges()
    {
        var ns = new Namespace("MyApp");
        var code = new Code(ns, "Console.WriteLine();");

        ns.Update("MyOtherApp");

        Assert.True(code.ValueChanged());
    }

    [Fact]
    public void ValueChanged_FalseWhenNamespaceIsNull()
    {
        var code = new Code(null, "Console.WriteLine();");

        Assert.False(code.ValueChanged());
    }

    // --- ToCodeString ---

    [Fact]
    public void ToCodeString_WithNamespaceAndValue()
    {
        var nsString = "Cool.App";
        var codeString = $"Console.WriteLine(\"Do?\");{Environment.NewLine}Console.ReadLine();";

        var code = new Code(new Namespace(nsString), codeString);

        Assert.Equal(
            $"namespace {nsString};{Environment.NewLine}{Environment.NewLine}{codeString}{Environment.NewLine}",
            code.ToCodeString());
    }

    [Fact]
    public void ToCodeString_WithoutNamespace_ReturnsCodeOnly()
    {
        var code = new Code("Console.WriteLine();");

        var result = code.ToCodeString();

        Assert.Contains("Console.WriteLine();", result);
        Assert.DoesNotContain("namespace", result);
    }

    [Fact]
    public void ToCodeString_WithNullValue_ReturnsNamespaceOnly()
    {
        var code = new Code(new Namespace("MyApp"), null);

        var result = code.ToCodeString();

        Assert.Contains("namespace MyApp;", result);
    }

    [Fact]
    public void ToCodeString_WithNullNamespaceAndNullValue_ReturnsEmpty()
    {
        var code = new Code(null, null);

        var result = code.ToCodeString();

        Assert.Equal(string.Empty, result);
    }
}
