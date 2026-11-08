using System.Text.Json;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;

namespace NetPad.Apps.Common.Tests.Data.EntityFrameworkCore.Scaffolding;

public class EntityFrameworkToolAssetsTests
{
    private const string LibraryPath = "microsoft.entityframeworkcore.tools/9.0.4";
    private const string EfDllFile = "tools/netcoreapp2.0/any/ef.dll";

    private static string AssetsJson(string[] packageFolders, object? libraries = null)
    {
        return JsonSerializer.Serialize(new
        {
            packageFolders = packageFolders.ToDictionary(folder => folder, _ => new { }),
            libraries = libraries ?? new Dictionary<string, object>
            {
                ["Microsoft.EntityFrameworkCore.Tools/9.0.4"] = new
                {
                    path = LibraryPath,
                    files = new[] { "tools/net472/any/ef.exe", EfDllFile, "tools/EntityFrameworkCore.psm1" }
                }
            }
        });
    }

    [Fact]
    public void Parse_ReturnsEveryPackageFolderInOrder()
    {
        string[] folders = ["/cache/", "/fallback/", "/other/"];

        var assets = EntityFrameworkToolAssets.Parse(AssetsJson(folders), _ => true);

        Assert.Equal(folders, assets.PackageFolders);
    }

    [Fact]
    public void Parse_UsesFirstPackageFolderThatHasEfDll()
    {
        var expected = Path.Combine("/fallback/", LibraryPath, EfDllFile);

        var assets = EntityFrameworkToolAssets.Parse(
            AssetsJson(["/cache/", "/fallback/", "/other/"]),
            path => path == expected || path.StartsWith("/other/"));

        Assert.Equal(expected, assets.EfDllPath);
    }

    [Theory]
    [InlineData("tools/netcoreapp2.0/any/ef.dll")]
    [InlineData("tools/net8.0/any/ef.dll")]
    [InlineData("tools/net10.0/any/ef.dll")]
    public void Parse_FindsEfDllInAnyFrameworkFolder(string efDllFile)
    {
        var libraries = new Dictionary<string, object>
        {
            ["Microsoft.EntityFrameworkCore.Tools/1.0.0"] = new
            {
                path = "microsoft.entityframeworkcore.tools/1.0.0",
                files = new[] { "tools/net461/any/ef.exe", "tools/net461/win-x86/ef.exe", efDllFile }
            }
        };

        var assets = EntityFrameworkToolAssets.Parse(AssetsJson(["/cache/"], libraries), _ => true);

        Assert.Equal(Path.Combine("/cache/", "microsoft.entityframeworkcore.tools/1.0.0", efDllFile), assets.EfDllPath);
    }

    [Fact]
    public void Parse_IgnoresOtherPackagesThatStartWithTheSameName()
    {
        var libraries = new Dictionary<string, object>
        {
            ["Microsoft.EntityFrameworkCore.Tools.Other/1.0.0"] = new
            {
                path = "other/1.0.0",
                files = new[] { "tools/any/ef.dll" }
            }
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => EntityFrameworkToolAssets.Parse(AssetsJson(["/cache/"], libraries), _ => true));

        Assert.Contains("Microsoft.EntityFrameworkCore.Tools", exception.Message);
    }

    [Fact]
    public void Parse_ThrowsWhenPackageIsNotReferenced()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => EntityFrameworkToolAssets.Parse(AssetsJson(["/cache/"], new { }), _ => true));

        Assert.Contains("does not reference", exception.Message);
    }

    [Fact]
    public void Parse_ThrowsWhenPackageHasNoEfDll()
    {
        var libraries = new Dictionary<string, object>
        {
            ["Microsoft.EntityFrameworkCore.Tools/1.0.0"] = new
            {
                path = "microsoft.entityframeworkcore.tools/1.0.0",
                files = new[] { "tools/net461/any/ef.exe" }
            }
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => EntityFrameworkToolAssets.Parse(AssetsJson(["/cache/"], libraries), _ => true));

        Assert.Contains("does not contain", exception.Message);
    }

    [Fact]
    public void Parse_ThrowsNamingTheFoldersWhenEfDllIsInNoPackageFolder()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => EntityFrameworkToolAssets.Parse(AssetsJson(["/cache/", "/fallback/"]), _ => false));

        Assert.Contains("/cache/", exception.Message);
        Assert.Contains("/fallback/", exception.Message);
    }

    [Fact]
    public void FromAssetsFile_ThrowsWhenFileDoesNotExist()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "project.assets.json");

        var exception = Assert.Throws<InvalidOperationException>(() => EntityFrameworkToolAssets.FromAssetsFile(path));

        Assert.Contains(path, exception.Message);
    }
}
