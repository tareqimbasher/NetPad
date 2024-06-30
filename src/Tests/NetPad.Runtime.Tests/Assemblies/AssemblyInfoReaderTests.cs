using NetPad.Assemblies;
using Xunit;

namespace NetPad.Runtime.Tests.Assemblies;

public class AssemblyInfoReaderTests
{
    [Fact]
    public void DeterminesIsManagedCorrectly()
    {
        var assembly = typeof(AssemblyInfoReader).Assembly;
        using var assemblyInfoReader = new AssemblyInfoReader(assembly.Location);

        Assert.True(assemblyInfoReader.IsManaged());
    }

    [Fact]
    public void ReturnsCorrectVersion()
    {
        var assembly = typeof(AssemblyInfoReader).Assembly;
        var expectedVersion = assembly.GetName().Version;
        using var assemblyInfoReader = new AssemblyInfoReader(assembly.Location);

        var actualVersion = assemblyInfoReader.GetVersion();

        Assert.Equal(expectedVersion, actualVersion);
    }

    [Fact]
    public void ReturnsCorrectAssemblyName()
    {
        var assembly = typeof(AssemblyInfoReader).Assembly;
        var expectedName = assembly.GetName().ToString();
        using var assemblyInfoReader = new AssemblyInfoReader(assembly.Location);

        var actualName = assemblyInfoReader.GetAssemblyName()?.ToString();

        Assert.Equal(expectedName, actualName);
    }

    [Fact]
    public void ReturnsCorrectNamespaces()
    {
        var assembly = typeof(AssemblyInfoReader).Assembly;
        var expectedNamespaces = assembly.GetTypes()
            .Where(t => t.IsPublic && t.Namespace != null)
            .Select(t => t.Namespace)
            .ToHashSet()
            .OrderBy(ns => ns);

        using var stream = File.OpenRead(assembly.Location);
        using var assemblyInfoReader = new AssemblyInfoReader(stream);

        var actualNamespaces = assemblyInfoReader
            .GetNamespaces()
            .OrderBy(ns => ns);

        Assert.Equal(expectedNamespaces, actualNamespaces);
    }
}
