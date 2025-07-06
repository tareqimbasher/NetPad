using NetPad.Utilities;
using Xunit;

namespace NetPad.Runtime.Tests.Utilities;

public class CollectionUtilTests
{
    [Fact]
    public void In()
    {
        string[] collection = ["a", "b", "c"];

        var result = CollectionUtil.In("a", collection);

        Assert.True(result);
    }

    [Fact]
    public void In_WithComparer()
    {
        string[] collection = ["a", "b", "c"];

        var result = CollectionUtil.In("A", collection, StringComparer.OrdinalIgnoreCase);

        Assert.True(result);
    }

    [Fact]
    public void NotIn()
    {
        string[] collection = ["a", "b", "c"];

        var result = CollectionUtil.NotIn("d", collection);

        Assert.True(result);
    }

    [Fact]
    public void NotIn_WithComparer()
    {
        string[] collection = ["a", "b", "c"];

        var result = CollectionUtil.NotIn("A", collection, StringComparer.Ordinal);

        Assert.True(result);
    }

    [Fact]
    public void HashSetAddRange()
    {
        HashSet<string> set = ["a", "b", "c"];
        string[] collection = ["a", "d", "e"];

        CollectionUtil.AddRange(set, collection);

        Assert.Equal(set, ["a", "b", "c", "d", "e"]);
    }
}
