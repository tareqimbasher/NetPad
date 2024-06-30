using NetPad.DotNet;
using Xunit;

namespace NetPad.Runtime.Tests.DotNet;

public class CodeTests
{
    [Fact]
    public void CodeEquality()
    {
        var code1 = new Code("Console.WriteLine();");
        var code2 = new Code("Console.WriteLine();");

        Assert.Equal(code1, code2);
    }

    [Fact]
    public void CodeEquality_HashSet()
    {
        var set = new HashSet<Code>();

        set.Add(new Code("Console.WriteLine();"));
        set.Add(new Code("Console.WriteLine();"));

        Assert.Single(set);
    }

    [Fact]
    public void UpdateValue_UpdatesValueChanged()
    {
        var code = new Code("Console.WriteLine();");

        Assert.False(code.ValueChanged());

        code.Update("Console.ReadLine();");

        Assert.True(code.ValueChanged());
    }

    [Fact]
    public void ToCodeString()
    {
        var nsString = "Cool.App";
        var codeString = $"Console.WriteLine(\"Do?\");{Environment.NewLine}Console.ReadLine();";

        var code = new Code(new Namespace(nsString), codeString);

        Assert.Equal(
            $"namespace {nsString};{Environment.NewLine}{Environment.NewLine}{codeString}{Environment.NewLine}",
            code.ToCodeString());
    }
}
