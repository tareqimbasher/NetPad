using NetPad.DotNet;

namespace NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;

/// <summary>
/// The locations <c>ef.dll</c> needs in order to run, as recorded by a restore in <c>project.assets.json</c>.
/// </summary>
/// <param name="EfDllPath">The full path to the <c>ef.dll</c> of the restored Tools package.</param>
/// <param name="PackageFolders">Every folder the project's packages were restored to or found in.</param>
public sealed record EntityFrameworkToolAssets(string EfDllPath, IReadOnlyList<string> PackageFolders)
{
    /// <summary>
    /// Reads the tool's locations from a project's <c>project.assets.json</c> file.
    /// </summary>
    /// <param name="assetsFilePath">The path to the <c>project.assets.json</c> file.</param>
    /// <exception cref="InvalidOperationException">Thrown when <c>ef.dll</c> cannot be found.</exception>
    public static EntityFrameworkToolAssets FromAssetsFile(string assetsFilePath)
    {
        return FromProjectAssets(ProjectAssetsFile.Read(assetsFilePath), File.Exists);
    }

    /// <summary>
    /// Reads the tool's locations from the contents of a <c>project.assets.json</c> file.
    /// </summary>
    /// <param name="assetsJson">The contents of the file.</param>
    /// <param name="fileExists">Determines whether a file exists at a path.</param>
    /// <exception cref="InvalidOperationException">Thrown when <c>ef.dll</c> cannot be found.</exception>
    public static EntityFrameworkToolAssets Parse(string assetsJson, Func<string, bool> fileExists)
    {
        return FromProjectAssets(ProjectAssetsFile.Parse(assetsJson), fileExists);
    }

    private static EntityFrameworkToolAssets FromProjectAssets(ProjectAssetsFile assets, Func<string, bool> fileExists)
    {
        // A package that's already in a NuGet fallback folder is used from there and never copied to the project's own
        // cache, so check every folder the restore recorded.
        var packageFolders = assets.PackageFolders;

        var (libraryPath, efDllFile) = FindEfDllInToolsLibrary(assets);

        var efDllPath = packageFolders
            .Select(folder => Path.Combine(folder, libraryPath, efDllFile))
            .FirstOrDefault(fileExists);

        if (efDllPath == null)
        {
            throw new InvalidOperationException(
                $"Could not locate ef.dll. '{libraryPath}/{efDllFile}' was not found in any of the project's " +
                $"package folders: {string.Join(", ", packageFolders)}");
        }

        return new EntityFrameworkToolAssets(efDllPath, packageFolders);
    }

    private static (string LibraryPath, string EfDllFile) FindEfDllInToolsLibrary(ProjectAssetsFile assets)
    {
        var library = assets.FindPackage(EntityFrameworkPackageUtils.ToolsPackageId)
                      ?? throw new InvalidOperationException(
                          $"Could not locate ef.dll. The project does not reference the package " +
                          $"'{EntityFrameworkPackageUtils.ToolsPackageId}'.");

        // The framework folder changes between package versions. The folders for Visual Studio's PowerShell
        // host hold ef.exe, so matching the file name is enough to pass over them.
        var efDllFile = library.Files
            .FirstOrDefault(file => file.EndsWith("/any/ef.dll", StringComparison.OrdinalIgnoreCase));

        if (efDllFile == null || library.Path == null)
        {
            throw new InvalidOperationException(
                $"Could not locate ef.dll. The package '{library.Id}/{library.Version}' does not contain one.");
        }

        return (library.Path, efDllFile);
    }
}
