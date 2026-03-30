using NetPad.DotNet.CodeAnalysis;

namespace NetPad.Runtime.Tests.DotNet.CodeAnalysis;

public class SourceCodeCollectionTests
{
    // --- GetAllUsings ---

    [Fact]
    public void GetAllUsings_CombinesUsingsFromAllSources()
    {
        var collection = new SourceCodeCollection
        {
            new("code1", ["System", "System.Text"]),
            new("code2", ["System.Linq", "System.Text"]) // System.Text is a duplicate
        };

        var usings = collection.GetAllUsings();

        Assert.Equal(3, usings.Count);
        Assert.Contains(usings, u => u.Value == "System");
        Assert.Contains(usings, u => u.Value == "System.Text");
        Assert.Contains(usings, u => u.Value == "System.Linq");
    }

    [Fact]
    public void GetAllUsings_ReturnsEmpty_WhenCollectionIsEmpty()
    {
        var collection = new SourceCodeCollection();

        var usings = collection.GetAllUsings();

        Assert.Empty(usings);
    }

    // --- GetAllCode ---

    [Fact]
    public void GetAllCode_CombinesCodeFromAllSources()
    {
        var collection = new SourceCodeCollection
        {
            new("Console.WriteLine();"),
            new("Console.ReadLine();")
        };

        var code = collection.GetAllCode();

        Assert.Contains("Console.WriteLine();", code);
        Assert.Contains("Console.ReadLine();", code);
    }

    [Fact]
    public void GetAllCode_ReturnsEmpty_WhenCollectionIsEmpty()
    {
        var collection = new SourceCodeCollection();

        var code = collection.GetAllCode();

        Assert.NotNull(code);
    }

    // --- ToCodeString ---

    [Fact]
    public void ToCodeString_IncludesUsingsAndCode()
    {
        var collection = new SourceCodeCollection
        {
            new("Console.WriteLine();", ["System"])
        };

        var result = collection.ToCodeString();

        Assert.Contains("using System;", result);
        Assert.Contains("Console.WriteLine();", result);
    }

    [Fact]
    public void ToCodeString_WithGlobalNotation_PrefixesUsings()
    {
        var collection = new SourceCodeCollection
        {
            new("code", ["System"])
        };

        var result = collection.ToCodeString(useGlobalUsingNotation: true);

        Assert.Contains("global using System;", result);
    }

    [Fact]
    public void ToCodeString_DeduplicatesUsingsAcrossSources()
    {
        var collection = new SourceCodeCollection
        {
            new("code1", ["System"]),
            new("code2", ["System"])
        };

        var result = collection.ToCodeString();

        // "using System;" should appear exactly once
        var count = result.Split("using System;").Length - 1;
        Assert.Equal(1, count);
    }

    // --- Constructor with collection ---

    [Fact]
    public void Constructor_WithCollection_PopulatesList()
    {
        var sources = new[] { new SourceCode("code1"), new SourceCode("code2") };

        var collection = new SourceCodeCollection(sources);

        Assert.Equal(2, collection.Count);
    }

    // --- Extension methods ---

    [Fact]
    public void ToSourceCodeCollection_CreatesNewInstance()
    {
        var original = new SourceCodeCollection { new("code") };

        var copy = original.ToSourceCodeCollection();

        Assert.NotSame(original, copy);
        Assert.Equal(original.Count, copy.Count);
    }
}
