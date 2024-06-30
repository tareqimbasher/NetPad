using NetPad.DotNet;
using Xunit;

namespace NetPad.Runtime.Tests.DotNet;

public class UsingTests
{
    [Fact]
    public void UsingEquality()
    {
        var using1 = new Using("System.Text");
        var using2 = new Using("System.Text");

        Assert.Equal(using1, using2);
    }

    [Fact]
    public void UsingEquality_HashSet()
    {
        var set = new HashSet<Using>();

        set.Add(new Using("System.Text"));
        set.Add(new Using("System.Text"));

        Assert.Single(set);
    }

    [Fact]
    public void UpdateValue_UpdatesValueChanged()
    {
        var using1 = new Using("System.Text");

        Assert.False(using1.ValueChanged());

        using1.Update("System");

        Assert.True(using1.ValueChanged());
    }

    [Fact]
    public void ToCodeString()
    {
        var using1 = new Using("System.Text");

        Assert.Equal("using System.Text;", using1.ToCodeString());
    }

    [Fact]
    public void ToCodeString_UsingGlobalAnnotation()
    {
        var using1 = new Using("System.Text");

        Assert.Equal("global using System.Text;", using1.ToCodeString(true));
    }

    [Fact]
    public void Implicit_String()
    {
        Using @explicit = new Using("System.Text");

        Using @implicit = "System.Text";

        Assert.Equal(@explicit, @implicit);
    }
}
