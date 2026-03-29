using NetPad.Utilities;

namespace NetPad.Runtime.Tests.Utilities;

public class TypeUtilTests
{
    [Fact]
    public void GetReadableName_SimpleType()
    {
        var name = typeof(int).GetReadableName();

        Assert.Equal("Int32", name);
    }

    [Fact]
    public void GetReadableName_SimpleType_WithNamespace()
    {
        var name = typeof(int).GetReadableName(withNamespace: true);

        Assert.Equal("System.Int32", name);
    }

    [Fact]
    public void GetReadableName_GenericType()
    {
        var name = typeof(List<string>).GetReadableName();

        Assert.Equal("List<String>", name);
    }

    [Fact]
    public void GetReadableName_GenericType_WithNamespace()
    {
        var name = typeof(List<string>).GetReadableName(withNamespace: true);

        Assert.Equal("System.Collections.Generic.List<System.String>", name);
    }

    [Fact]
    public void GetReadableName_NestedGenericType()
    {
        var name = typeof(Dictionary<string, List<int>>).GetReadableName();

        Assert.Equal("Dictionary<String, List<Int32>>", name);
    }

    [Fact]
    public void GetReadableName_NullableValueType()
    {
        var name = typeof(int?).GetReadableName();

        Assert.Equal("Int32?", name);
    }

    [Fact]
    public void GetReadableName_NullableValueType_WithNamespace()
    {
        var name = typeof(int?).GetReadableName(withNamespace: true);

        Assert.Equal("System.Int32?", name);
    }

    [Fact]
    public void GetReadableName_ForHtml_EscapesAngleBrackets()
    {
        var name = typeof(List<string>).GetReadableName(forHtml: true);

        Assert.Equal("List&lt;String&gt;", name);
    }

    [Fact]
    public void GetReadableName_ForHtml_WithNamespace()
    {
        var name = typeof(List<string>).GetReadableName(withNamespace: true, forHtml: true);

        Assert.Equal("System.Collections.Generic.List&lt;System.String&gt;", name);
    }

    [Fact]
    public void IsOfGenericType_ReturnsTrueForDirectMatch()
    {
        Assert.True(typeof(List<int>).IsOfGenericType(typeof(List<>)));
    }

    [Fact]
    public void IsOfGenericType_ReturnsFalseForNonGenericType()
    {
        Assert.False(typeof(string).IsOfGenericType(typeof(List<>)));
    }

    [Fact]
    public void IsOfGenericType_ReturnsFalseForDifferentGenericType()
    {
        Assert.False(typeof(List<int>).IsOfGenericType(typeof(Dictionary<,>)));
    }

    [Fact]
    public void IsOfGenericType_ChecksBaseTypes()
    {
        Assert.True(typeof(CustomList).IsOfGenericType(typeof(List<>)));
    }

    private class CustomList : List<string>;
}
