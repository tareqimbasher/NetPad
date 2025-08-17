using System.Xml.Linq;
using NetPad.Configuration;
using NetPad.DotNet;
using Xunit;

namespace NetPad.Runtime.Tests.DotNet;

public partial class DotNetCSharpProjectTests : IAsyncLifetime
{
    private readonly string _root;
    private readonly string _projectDir;
    private readonly string _packageCacheDir;

    public DotNetCSharpProjectTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "DotNetCSharpProjectTests", Guid.NewGuid().ToString("N"));
        _projectDir = Path.Combine(_root, "proj");
        _packageCacheDir = Path.Combine(_root, "pkgcache");
        Directory.CreateDirectory(_projectDir);
        Directory.CreateDirectory(_packageCacheDir);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch
        {
            // Ignore
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateAsync_WritesExpectedCsproj()
    {
        var proj = NewProject();

        await proj.CreateAsync(
            targetDotNetFrameworkVersion: DotNetFrameworkVersion.DotNet8,
            outputType: ProjectOutputType.Executable,
            sdkPack: DotNetSdkPack.NetApp,
            enableNullable: true,
            enableImplicitUsings: false);

        var csproj = XDocument.Load(proj.ProjectFilePath.Path);
        var root = csproj.Element("Project");
        Assert.NotNull(root);
        Assert.Equal("Microsoft.NET.Sdk", root.Attribute("Sdk")?.Value);

        var pg = root.Element("PropertyGroup");
        Assert.NotNull(pg);
        Assert.Equal("Exe", pg.Element("OutputType")?.Value);
        Assert.Equal(DotNetFrameworkVersion.DotNet8.GetTargetFrameworkMoniker(), pg.Element("TargetFramework")?.Value);
        Assert.Equal("enable", pg.Element("Nullable")?.Value);
        Assert.Equal("disable", pg.Element("ImplicitUsings")?.Value);
    }

    [Fact]
    public async Task CreateAsync_AppendsCsprojExtension_WhenMissing()
    {
        var proj = NewProject("myproject");

        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Library);

        Assert.True(File.Exists(Path.Combine(_projectDir, "myproject.csproj")));
    }

    [Fact]
    public async Task CreateAsync_DoesNotAppendCsprojExtension_WhenExists()
    {
        var proj = NewProject("myproject.csproj");

        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Library);

        Assert.True(File.Exists(Path.Combine(_projectDir, "myproject.csproj")));
    }

    [Fact]
    public async Task DeleteAsync_RemovesProjectDirectory()
    {
        var proj = NewProject();
        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Library);

        await proj.DeleteAsync();

        Assert.False(Directory.Exists(_projectDir));
    }

    [Fact]
    public async Task ModifyProjectFileAsync_AppliesCustomChange()
    {
        var proj = NewProject();
        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Library);

        await proj.ModifyProjectFileAsync(root =>
        {
            var pg = root.Element("PropertyGroup")!;
            pg.Add(new XElement("LangVersion", "latest"));
        });

        var doc = XDocument.Load(proj.ProjectFilePath.Path);
        Assert.Equal("latest", doc.Descendants("LangVersion").First().Value);
    }

    [Fact]
    public async Task SetProjectAttributeAsync_SetsAttributeOnProject()
    {
        var proj = NewProject();
        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Library);

        await proj.SetProjectAttributeAsync("Sdk", "Microsoft.NET.Sdk.Web");

        var doc = XDocument.Load(proj.ProjectFilePath.Path);
        Assert.Equal("Microsoft.NET.Sdk.Web", doc.Root!.Attribute("Sdk")!.Value);
    }

    [Fact]
    public async Task SetProjectGroupItemAsync_AddsNewElement_WhenMissing()
    {
        var proj = NewProject();
        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Library, enableNullable: true);

        await proj.SetProjectGroupItemAsync("Nullable", "disable"); // update existing
        await proj.SetProjectGroupItemAsync("TreatWarningsAsErrors", "true"); // add new

        var doc = XDocument.Load(proj.ProjectFilePath.Path);
        var pg = doc.Root!.Element("PropertyGroup")!;
        Assert.Equal("disable", pg.Element("Nullable")!.Value);
        Assert.Equal("true", pg.Element("TreatWarningsAsErrors")!.Value);
    }

    [Fact]
    public async Task SetProjectGroupItemAsync_UpdatesElement_WhenExists()
    {
        var proj = NewProject();
        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Library, enableNullable: true);

        await proj.SetProjectGroupItemAsync("TreatWarningsAsErrors", "false"); // adds new
        await proj.SetProjectGroupItemAsync("TreatWarningsAsErrors", "true"); // updates existing

        var doc = XDocument.Load(proj.ProjectFilePath.Path);
        var pg = doc.Root!.Element("PropertyGroup")!;
        Assert.Equal("true", pg.Element("TreatWarningsAsErrors")!.Value);
    }

    [Fact]
    public async Task SetProjectGroupItemAsync_Throws_WhenNoPropertyGroup()
    {
        var proj = NewProject();
        // Write a malformed project (no PropertyGroup)
        var xml = """
                  <Project Sdk="Microsoft.NET.Sdk"></Project>
                  """;
        await File.WriteAllTextAsync(proj.ProjectFilePath.Path, xml);

        await Assert.ThrowsAsync<FormatException>(() =>
            proj.SetProjectGroupItemAsync("Nullable", "enable"));
    }

    [Fact]
    public async Task RestoreAsync_InvokesDotnetAndCapturesOutput()
    {
        var proj = NewProject();
        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Library);

        var result = await proj.RestoreAsync();

        Assert.True(result.Succeeded);
        Assert.Contains("restore", result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Path.GetFileName(proj.ProjectFilePath.Path), result.Output);
        Assert.True(File.Exists(proj.ProjectDirectoryPath.CombineFilePath("obj", "project.assets.json").Path));
    }

    [Fact]
    public async Task BuildAsync_InvokesDotnetAndCapturesOutput()
    {
        var proj = NewProject();
        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Library);

        var result = await proj.BuildAsync();

        Assert.True(result.Succeeded);
        Assert.Contains("build", result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Path.GetFileName(proj.ProjectFilePath.Path), result.Output);
    }

    [Fact]
    public async Task RunAsync_InvokesDotnetAndCapturesOutput()
    {
        var proj = NewProject(includePackageCachePath: true);
        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Executable);
        await File.WriteAllTextAsync(
            proj.ProjectDirectoryPath.CombineFilePath("Program.cs").Path,
            "System.Console.WriteLine(\"Hello World!\");");

        var result = await proj.RunAsync();

        Assert.True(result.Succeeded, result.Output);
        Assert.Contains("Hello World!", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetProjectSdkName_ReturnsExpected()
    {
        Assert.Equal("Microsoft.NET.Sdk", DotNetCSharpProject.GetProjectSdkName(DotNetSdkPack.NetApp));
        Assert.Equal("Microsoft.NET.Sdk.Web", DotNetCSharpProject.GetProjectSdkName(DotNetSdkPack.AspNetApp));
    }

    [Fact]
    public async Task BinDirectoryPath_IsUnderProjectDir()
    {
        var proj = NewProject();
        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Library);

        Assert.Equal(Path.Combine(_projectDir, "bin"), proj.BinDirectoryPath.Path);
    }

    private DotNetCSharpProject NewProject(
        string? projectFileName = null,
        bool includePackageCachePath = true)
    {
        var info = new DotNetInfo(new Settings());
        var cacheDir = includePackageCachePath ? _packageCacheDir : null;

        return new DotNetCSharpProject(
            info,
            _projectDir,
            projectFileName ?? "project",
            cacheDir);
    }
}
