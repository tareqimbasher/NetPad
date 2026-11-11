using System.Text.Json;
using NetPad.DotNet;

namespace NetPad.Runtime.Tests.DotNet;

public class ProjectAssetsFileTests
{
    private const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";

    private static string AssetsJson(params string[] libraries)
    {
        return JsonSerializer.Serialize(new
        {
            packageFolders = new Dictionary<string, object> { ["/cache/"] = new { } },
            libraries = libraries.ToDictionary(
                library => library,
                library => new { type = "package", path = library.ToLowerInvariant(), files = Array.Empty<string>() })
        });
    }

    [Fact]
    public void GetPackageVersion_ReturnsVersionOfDirectReference()
    {
        var assets = ProjectAssetsFile.Parse(AssetsJson(
            "Microsoft.EntityFrameworkCore/9.0.23",
            "Microsoft.EntityFrameworkCore.Abstractions/9.0.23",
            "Microsoft.EntityFrameworkCore.Relational/9.0.23",
            "Microsoft.EntityFrameworkCore.Sqlite/9.0.23"));

        Assert.Equal("9.0.23", assets.GetPackageVersion(EntityFrameworkCore));
    }

    [Fact]
    public void GetPackageVersion_ReturnsVersionOfPackageThatComesThroughAnotherPackage()
    {
        var assets = ProjectAssetsFile.Parse(AssetsJson(
            "Microsoft.EntityFrameworkCore.Relational/10.0.0",
            "Oracle.EntityFrameworkCore/10.23.26400",
            "Microsoft.EntityFrameworkCore/10.0.0",
            "Microsoft.EntityFrameworkCore.Abstractions/10.0.0"));

        Assert.Equal("10.0.0", assets.GetPackageVersion(EntityFrameworkCore));
    }

    [Fact]
    public void GetPackageVersion_IgnoresCase()
    {
        var assets = ProjectAssetsFile.Parse(AssetsJson("microsoft.entityframeworkcore/8.0.34"));

        Assert.Equal("8.0.34", assets.GetPackageVersion(EntityFrameworkCore));
    }

    [Fact]
    public void GetPackageVersion_ThrowsNamingThePackage_WhenRestoreDidNotResolveIt()
    {
        var assets = ProjectAssetsFile.Parse(AssetsJson(
            "Microsoft.EntityFrameworkCore.Abstractions/9.0.23",
            "Microsoft.EntityFrameworkCore.Relational/9.0.23"));

        var exception = Assert.Throws<InvalidOperationException>(
            () => assets.GetPackageVersion(EntityFrameworkCore));

        Assert.Contains($"'{EntityFrameworkCore}'", exception.Message);
    }

    [Fact]
    public void Parse_ReadsEachLibrary()
    {
        var json = JsonSerializer.Serialize(new
        {
            libraries = new Dictionary<string, object>
            {
                ["Microsoft.EntityFrameworkCore.Tools/9.0.4"] = new
                {
                    type = "package",
                    path = "microsoft.entityframeworkcore.tools/9.0.4",
                    files = new[] { "tools/netcoreapp2.0/any/ef.dll" }
                },
                ["MyProject/1.0.0"] = new { type = "project" }
            }
        });

        var assets = ProjectAssetsFile.Parse(json);

        Assert.Empty(assets.PackageFolders);
        Assert.Collection(
            assets.Libraries,
            tools =>
            {
                Assert.Equal("Microsoft.EntityFrameworkCore.Tools", tools.Id);
                Assert.Equal("9.0.4", tools.Version);
                Assert.Equal("microsoft.entityframeworkcore.tools/9.0.4", tools.Path);
                Assert.Equal(["tools/netcoreapp2.0/any/ef.dll"], tools.Files);
            },
            project =>
            {
                Assert.Equal("MyProject", project.Id);
                Assert.Null(project.Path);
                Assert.Empty(project.Files);
            });
    }

    [Fact]
    public void Read_ThrowsWhenFileDoesNotExist()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "project.assets.json");

        var exception = Assert.Throws<InvalidOperationException>(() => ProjectAssetsFile.Read(path));

        Assert.Contains(path, exception.Message);
    }
}
