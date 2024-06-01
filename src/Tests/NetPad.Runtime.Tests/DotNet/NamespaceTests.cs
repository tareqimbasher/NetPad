using NetPad.DotNet;
using Xunit;

namespace NetPad.Runtime.Tests.DotNet;

public class NamespaceTests
{
    [Fact]
    public void NamespaceEquality()
    {
        var ns1 = new Namespace("System.Text");
        var ns2 = new Namespace("System.Text");

        Assert.Equal(ns1, ns2);
    }

    [Fact]
    public void NamespaceEquality_HashSet()
    {
        var set = new HashSet<Namespace>();

        set.Add(new Namespace("System.Text"));
        set.Add(new Namespace("System.Text"));

        Assert.Single(set);
    }

    [Fact]
    public void UpdateValue_UpdatesValueChanged()
    {
        var ns = new Namespace("System.Text");

        Assert.False(ns.ValueChanged());

        ns.Update("System");

        Assert.True(ns.ValueChanged());
    }

    [Fact]
    public void ToCodeString()
    {
        var ns = new Namespace("System.Text");

        Assert.Equal("namespace System.Text;", ns.ToCodeString());
    }

    [Fact]
    public void Implicit_String()
    {
        Namespace @explicit = new Namespace("System.Text");

        Namespace @implicit = "System.Text";

        Assert.Equal(@explicit, @implicit);
    }
}
