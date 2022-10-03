using System.IO;
using System.Linq;
using NetPad.Assemblies;
using Xunit;

namespace NetPad.Domain.Tests.Assemblies;

public class AssemblyInfoReaderTests
{
    [Fact]
    public void ReturnsNamespaces()
    {
        var assemblyInfoReader = new AssemblyInfoReader();
        var assembly = typeof(AssemblyInfoReader).Assembly;
        var bytes = File.ReadAllBytes(assembly.Location);
        var expectedNamespaces = assembly.GetTypes()
            .Where(t => t.IsPublic && t.Namespace != null)
            .Select(t => t.Namespace)
            .ToHashSet()
            .OrderBy(ns => ns);

        var actualNamespaces = assemblyInfoReader
            .GetNamespaces(bytes)
            .OrderBy(ns => ns);

        Assert.Equal(expectedNamespaces, actualNamespaces);
    }
}
