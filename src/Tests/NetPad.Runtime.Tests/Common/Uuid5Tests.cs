using NetPad.Common;
using Xunit;

namespace NetPad.Runtime.Tests.Common;

public class Uuid5Tests
{
    private static readonly Guid _rfcDnsNamespace = new("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

    [Fact]
    public void Create_WithSameNameAndDefaultNamespace_IsDeterministic()
    {
        var name = "hello-world";
        var a = Uuid5.Create(name);
        var b = Uuid5.Create(name);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Create_WithDifferentNames_ProducesDifferentGuids()
    {
        var a = Uuid5.Create("alpha");
        var b = Uuid5.Create("beta");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Create_WithDifferentNamespaces_ProducesDifferentGuids()
    {
        var name = "same-name";
        var ns1 = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
        var ns2 = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");

        var a = Uuid5.Create(ns1, name);
        var b = Uuid5.Create(ns2, name);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Create_DefaultOverload_UsesAppNamespace()
    {
        var name = "check-default-namespace";
        var expected = Uuid5.Create(Uuid5.AppNamespace, name);
        var actual = Uuid5.Create(name);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Create_SetsVersionTo5_AndVariantToRfc4122()
    {
        var g = Uuid5.Create("version-and-variant-check");
        var s = g.ToString("D"); // xxxxxxxx-xxxx-Mxxx-Nxxx-xxxxxxxxxxxx
        var parts = s.Split('-');

        // Version nibble is the first hex digit of the 3rd group
        int version = Convert.ToInt32(parts[2][0].ToString(), 16);
        Assert.Equal(5, version);

        // Variant nibble is the first hex digit of the 4th group: must be 8, 9, a, or b
        char variantNibble = char.ToLowerInvariant(parts[3][0]);
        Assert.Contains(variantNibble, new[] { '8', '9', 'a', 'b' });
    }

    [Fact]
    public void Create_Rfc4122_Dns_WwwExampleCom_MatchesKnownVector()
    {
        // v5(name="www.example.com", ns=DNS) = 2ed6657d-e927-568b-95e1-2665a8aea6a2
        var uuid = Uuid5.Create(_rfcDnsNamespace, "www.example.com");
        Assert.Equal("2ed6657d-e927-568b-95e1-2665a8aea6a2", uuid.ToString("D"));
    }

    [Fact]
    public void Create_ThrowsOnNullName()
    {
        Assert.Throws<ArgumentNullException>(() => Uuid5.Create(null!));
        Assert.Throws<ArgumentNullException>(() => Uuid5.Create(Guid.Empty, null!));
    }
}
