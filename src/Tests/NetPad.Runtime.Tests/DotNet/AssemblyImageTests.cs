using System.Reflection;
using NetPad.DotNet;
using Xunit;

namespace NetPad.Runtime.Tests.DotNet;

public class AssemblyImageTests
{
    [Fact]
    public void ConstructorThrowsIfEmptyImage()
    {
        Assert.Throws<ArgumentNullException>(() => new AssemblyImage(new AssemblyName("Foo"), []));
    }

    [Theory]
    [InlineData("Foo.Bar.Baz", "Foo.Bar.Baz.dll")]
    [InlineData("Foo.Bar.Baz, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Foo.Bar.Baz.dll")]
    [InlineData("Foo.Bar.Baz.dll", "Foo.Bar.Baz.dll")]
    [InlineData("Foo.Bar/Baz", "Foo.BarBaz.dll")]
    public void ConstructAssemblyFileNameTests(string assemblyName, string expectedName)
    {
        var assemblyImage = new AssemblyImage(new AssemblyName(assemblyName), [0]);

        var fileName = assemblyImage.ConstructAssemblyFileName();

        Assert.Equal(expectedName, fileName);
    }
}
