using System.Reflection;
using System.Xml.Linq;
using NetPad.DotNet;
using NetPad.DotNet.References;
using Xunit;

namespace NetPad.Runtime.Tests.DotNet;

public partial class DotNetCSharpProjectTests
{
    [Fact]
    public async Task AddReferenceAsync_AssemblyFile_AddsReferenceNodeWithHintPath_TracksReference()
    {
        var proj = NewProject();
        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Library);

        var assemblyPath = typeof(object).Assembly.Location;
        var reference = new AssemblyFileReference(assemblyPath);

        await proj.AddReferenceAsync(reference);

        var doc = XDocument.Load(proj.ProjectFilePath.Path);
        var referenceElement = FindAssemblyReferenceElement(doc, assemblyPath);

        Assert.NotNull(referenceElement);
        Assert.Equal(
            AssemblyName.GetAssemblyName(assemblyPath).FullName,
            referenceElement.Attribute("Include")?.Value);
        Assert.Equal(assemblyPath, referenceElement.Element("HintPath")?.Value);

        // Ensure we track it in the in-memory set
        Assert.Contains(reference, proj.References);
    }

    [Fact]
    public async Task AddReferenceAsync_AssemblyFile_IsIdempotent()
    {
        var proj = NewProject();
        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Library);

        var assemblyPath = typeof(object).Assembly.Location;
        var reference = new AssemblyFileReference(assemblyPath);

        await proj.AddReferenceAsync(reference);
        await proj.AddReferenceAsync(reference); // second call should be a no-op

        var doc = XDocument.Load(proj.ProjectFilePath.Path);
        var allRefNodes = doc
            .Root!
            .Elements("ItemGroup")
            .SelectMany(g => g.Elements("Reference"))
            .Where(r => r.Elements("HintPath").Any(h => h.Value == assemblyPath))
            .ToList();

        Assert.Single(allRefNodes); // no duplicates
    }

    [Fact]
    public async Task RemoveReferenceAsync_AssemblyFile_RemovesReferenceNode_UntracksReference()
    {
        var proj = NewProject();
        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Library);

        var assemblyPath = typeof(object).Assembly.Location;
        var reference = new AssemblyFileReference(assemblyPath);

        await proj.AddReferenceAsync(reference);

        // Sanity check: make sure reference was added
        Assert.NotNull(FindAssemblyReferenceElement(XDocument.Load(proj.ProjectFilePath.Path), assemblyPath));

        await proj.RemoveReferenceAsync(reference);

        var doc = XDocument.Load(proj.ProjectFilePath.Path);
        Assert.Null(FindAssemblyReferenceElement(doc, assemblyPath));
        Assert.DoesNotContain(reference, proj.References);

        // In-memory tracking updated
        Assert.DoesNotContain(reference, proj.References);
    }

    [Fact]
    public async Task RemoveReferenceAsync_AssemblyFile_NoOpWhenMissing()
    {
        var proj = NewProject();
        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Library);

        var assemblyPath = typeof(string).Assembly.Location;
        var reference = new AssemblyFileReference(assemblyPath);

        await proj.RemoveReferenceAsync(reference);

        var doc = XDocument.Load(proj.ProjectFilePath.Path);
        Assert.Null(FindAssemblyReferenceElement(doc, assemblyPath));
        Assert.Empty(proj.References);
    }

    [Fact]
    public async Task AddReferenceAsync_AssemblyImage_WritesFile_AddsReferenceNode_TracksReference()
    {
        var proj = NewProject();
        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Library);

        var (reference, expectedPath, expectedInclude) = MakeAssemblyImageReference();

        await proj.AddReferenceAsync(reference);

        // Image written to disk
        Assert.True(File.Exists(expectedPath));

        // Reference node added with Include + HintPath
        var doc = XDocument.Load(proj.ProjectFilePath.Path);
        var referenceElement = FindAssemblyReferenceElement(doc, expectedPath);
        Assert.NotNull(referenceElement);
        Assert.Equal(expectedInclude, referenceElement.Attribute("Include")?.Value);
        Assert.Equal(expectedPath, referenceElement.Element("HintPath")?.Value);

        // In-memory tracking updated
        Assert.Contains(reference, proj.References);
    }

    [Fact]
    public async Task AddReferenceAsync_AssemblyImage_IsIdempotent_NoDuplicateXmlOrFiles()
    {
        var proj = NewProject();
        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Library);

        var (reference, expectedPath, _) = MakeAssemblyImageReference();

        await proj.AddReferenceAsync(reference);
        var firstWriteTime = File.GetLastWriteTimeUtc(expectedPath);

        await Task.Delay(100); // ensure there's enough time for a different timestamp

        // 2nd call should be a no-op
        await proj.AddReferenceAsync(reference);

        // Still only one matching <Reference> node
        var doc = XDocument.Load(proj.ProjectFilePath.Path);
        var allRefNodes = doc
            .Root!
            .Elements("ItemGroup")
            .SelectMany(g => g.Elements("Reference"))
            .Where(r => r.Elements("HintPath").Any(h => h.Value == expectedPath))
            .ToList();
        Assert.Single(allRefNodes);

        // File isn't rewritten
        Assert.Equal(firstWriteTime, File.GetLastWriteTimeUtc(expectedPath));
    }

    [Fact]
    public async Task RemoveReferenceAsync_AssemblyImage_RemovesFile_RemovesReferenceNode_UntracksReference()
    {
        var proj = NewProject();
        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Library);

        var (reference, expectedPath, _) = MakeAssemblyImageReference();

        await proj.AddReferenceAsync(reference);

        // Sanity checks
        Assert.True(File.Exists(expectedPath));
        Assert.NotNull(FindAssemblyReferenceElement(XDocument.Load(proj.ProjectFilePath.Path), expectedPath));
        Assert.Contains(reference, proj.References);

        await proj.RemoveReferenceAsync(reference);

        // File deleted
        Assert.False(File.Exists(expectedPath));

        // XML node removed
        var doc = XDocument.Load(proj.ProjectFilePath.Path);
        Assert.Null(FindAssemblyReferenceElement(doc, expectedPath));

        // In-memory tracking updated
        Assert.DoesNotContain(reference, proj.References);
    }

    [Fact]
    public async Task RemoveReferenceAsync_AssemblyImage_NoOpWhenMissing()
    {
        var proj = NewProject();
        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Library);

        var (reference, expectedPath, _) = MakeAssemblyImageReference();

        await proj.RemoveReferenceAsync(reference);

        Assert.False(File.Exists(expectedPath));
        var doc = XDocument.Load(proj.ProjectFilePath.Path);
        Assert.Null(FindAssemblyReferenceElement(doc, expectedPath));
        Assert.Empty(proj.References);
    }

    private static XElement? FindAssemblyReferenceElement(XDocument doc, string assemblyPath)
    {
        var root = doc.Root!;
        return root
            .Elements("ItemGroup")
            .SelectMany(g => g.Elements("Reference"))
            .FirstOrDefault(r =>
                r.Elements("HintPath").Any(h => string.Equals(h.Value, assemblyPath, StringComparison.Ordinal)));
    }

    /// <summary>
    /// Creates a valid AssemblyImageReference from a real framework assembly so that
    /// AssemblyName.GetAssemblyName() succeeds when project code inspects the file we write.
    /// </summary>
    private (AssemblyImageReference Reference, string ExpectedPath, string ExpectedInclude) MakeAssemblyImageReference()
    {
        var runtimeAssemblyPath = typeof(object).Assembly.Location; // System.Private.CoreLib.dll
        var asmName = AssemblyName.GetAssemblyName(runtimeAssemblyPath);
        var imageBytes = File.ReadAllBytes(runtimeAssemblyPath);

        var image = new AssemblyImage(asmName, imageBytes);
        var reference = new AssemblyImageReference(image);

        var expectedFileName = image.ConstructAssemblyFileName(); // e.g., "System.Private.CoreLib.dll"
        var expectedPath = Path.Combine(_projectDir, expectedFileName);
        var expectedInclude = asmName.FullName;

        return (reference, expectedPath, expectedInclude);
    }
}
