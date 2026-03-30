using NetPad.DotNet.CodeAnalysis;

namespace NetPad.Runtime.Tests.DotNet.CodeAnalysis;

public class SourceCodeTests
{
    // --- Constructors ---

    [Fact]
    public void Constructor_WithCode_SetsCode()
    {
        var source = new SourceCode("Console.WriteLine();");

        Assert.Equal("Console.WriteLine();", source.Code.Value);
    }

    [Fact]
    public void Constructor_WithNullCode_CreatesEmptyCode()
    {
        var source = new SourceCode(code: (string?)null);

        Assert.NotNull(source.Code);
        Assert.Null(source.Code.Value);
    }

    [Fact]
    public void Constructor_WithUsings_SetsUsings()
    {
        var source = new SourceCode("code", ["System", "System.Text"]);

        Assert.Equal(2, source.Usings.Count());
    }

    [Fact]
    public void Constructor_WithUsingObjects_SetsUsings()
    {
        var usings = new[] { new Using("System"), new Using("System.Text") };
        var source = new SourceCode(new Code("code"), usings);

        Assert.Equal(2, source.Usings.Count());
    }

    [Fact]
    public void Constructor_WithUsingsOnly_HasNullCodeValue()
    {
        var source = new SourceCode(new[] { "System" });

        Assert.Null(source.Code.Value);
    }

    // --- AddUsing / RemoveUsing ---

    [Fact]
    public void AddUsing_AddsToCollection()
    {
        var source = new SourceCode("code");

        source.AddUsing("System.Text");

        Assert.Contains(source.Usings, u => u.Value == "System.Text");
    }

    [Fact]
    public void AddUsing_DuplicateIsIgnored()
    {
        var source = new SourceCode("code", ["System.Text"]);

        source.AddUsing("System.Text");

        Assert.Single(source.Usings);
    }

    [Fact]
    public void AddUsing_SetsValueChanged()
    {
        var source = new SourceCode("code");
        Assert.False(source.ValueChanged());

        source.AddUsing("System.Text");

        Assert.True(source.ValueChanged());
    }

    [Fact]
    public void AddUsing_Duplicate_DoesNotSetValueChanged()
    {
        var source = new SourceCode("code", ["System.Text"]);

        source.AddUsing("System.Text");

        Assert.False(source.ValueChanged());
    }

    [Fact]
    public void RemoveUsing_RemovesFromCollection()
    {
        var source = new SourceCode("code", ["System", "System.Text"]);

        source.RemoveUsing("System.Text");

        Assert.Single(source.Usings);
        Assert.DoesNotContain(source.Usings, u => u.Value == "System.Text");
    }

    [Fact]
    public void RemoveUsing_SetsValueChanged()
    {
        var source = new SourceCode("code", ["System.Text"]);

        source.RemoveUsing("System.Text");

        Assert.True(source.ValueChanged());
    }

    [Fact]
    public void RemoveUsing_NonExistent_DoesNotSetValueChanged()
    {
        var source = new SourceCode("code", ["System"]);

        source.RemoveUsing("System.Text");

        Assert.False(source.ValueChanged());
    }

    // --- ValueChanged ---

    [Fact]
    public void ValueChanged_FalseInitially()
    {
        var source = new SourceCode("code", ["System"]);

        Assert.False(source.ValueChanged());
    }

    [Fact]
    public void ValueChanged_TrueWhenCodeUpdated()
    {
        var source = new SourceCode("code");

        source.Code.Update("new code");

        Assert.True(source.ValueChanged());
    }

    [Fact]
    public void ValueChanged_TrueWhenUsingUpdated()
    {
        var source = new SourceCode("code", ["System"]);

        source.Usings.First().Update("System.Text");

        Assert.True(source.ValueChanged());
    }

    // --- ToCodeString ---

    [Fact]
    public void ToCodeString_IncludesUsingsAndCode()
    {
        var source = new SourceCode("Console.WriteLine();", ["System"]);

        var result = source.ToCodeString();

        Assert.Contains("using System;", result);
        Assert.Contains("Console.WriteLine();", result);
    }

    [Fact]
    public void ToCodeString_WithGlobalNotation_PrefixesUsings()
    {
        var source = new SourceCode("code", ["System"]);

        var result = source.ToCodeString(useGlobalNotation: true);

        Assert.Contains("global using System;", result);
    }

    // --- Parse ---

    [Fact]
    public void Parse_ExtractsUsings()
    {
        var text = """
                   using System;
                   using System.Text.Json;
                   using System.Text
                   .Json;
                   using System.Threading
                   .Tasks;
                   using enc = System.Text.Encoding;
                   using enc2 = System.Text
                   .Encoding;

                   namespace MyApp.Utils;

                   public class Car
                   {
                        public string Name { get; }
                   }

                   public enum Color
                   {
                        Red, Blue, Green
                   }
                   """;

        var sourceCode = SourceCode.Parse(text);

        Assert.Equal(
            [
                "System",
                "System.Text.Json",
                "System.Threading.Tasks",
                "enc = System.Text.Encoding",
                "enc2 = System.Text.Encoding",
            ],
            sourceCode.Usings.Select(x => x.Value));
    }

    [Fact]
    public void Parse_ExtractsCode()
    {
        var text = """
                   using System;

                   public class Foo
                   {
                       public int Bar { get; }
                   }
                   """;

        var source = SourceCode.Parse(text);

        Assert.Contains("class Foo", source.Code.Value);
        Assert.Contains("Bar", source.Code.Value);
    }

    [Fact]
    public void Parse_HandlesNoUsings()
    {
        var text = "public class Foo { }";

        var source = SourceCode.Parse(text);

        Assert.Empty(source.Usings);
        Assert.Contains("class Foo", source.Code.Value);
    }

    [Fact]
    public void Parse_HandlesAliasUsings()
    {
        var text = """
                   using enc = System.Text.Encoding;

                   public class Foo { }
                   """;

        var source = SourceCode.Parse(text);

        Assert.Contains(source.Usings, u => u.Value == "enc = System.Text.Encoding");
    }
}
