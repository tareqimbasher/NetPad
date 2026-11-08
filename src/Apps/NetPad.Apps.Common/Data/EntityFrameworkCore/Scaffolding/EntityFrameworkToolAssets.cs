using System.Text.Json;

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
        if (!File.Exists(assetsFilePath))
        {
            throw new InvalidOperationException(
                $"Could not locate ef.dll. The project has not been restored: '{assetsFilePath}' does not exist.");
        }

        return Parse(File.ReadAllText(assetsFilePath), File.Exists);
    }

    /// <summary>
    /// Reads the tool's locations from the contents of a <c>project.assets.json</c> file.
    /// </summary>
    /// <param name="assetsJson">The contents of the file.</param>
    /// <param name="fileExists">Determines whether a file exists at a path.</param>
    /// <exception cref="InvalidOperationException">Thrown when <c>ef.dll</c> cannot be found.</exception>
    public static EntityFrameworkToolAssets Parse(string assetsJson, Func<string, bool> fileExists)
    {
        using var document = JsonDocument.Parse(assetsJson);
        var root = document.RootElement;

        // A package that's already in a NuGet fallback folder is used from there and never copied to the project's own
        // cache, so check every folder the restore recorded.
        var packageFolders = root.TryGetProperty("packageFolders", out var folders)
            ? folders.EnumerateObject().Select(folder => folder.Name).ToArray()
            : [];

        var (libraryPath, efDllFile) = FindEfDllInToolsLibrary(root);

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

    private static (string LibraryPath, string EfDllFile) FindEfDllInToolsLibrary(JsonElement root)
    {
        if (root.TryGetProperty("libraries", out var libraries))
        {
            foreach (var library in libraries.EnumerateObject())
            {
                if (!library.Name.StartsWith($"{EntityFrameworkPackageUtils.ToolsPackageId}/",
                                             StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // The framework folder changes between package versions. The folders for Visual Studio's PowerShell
                // host hold ef.exe, so matching the file name is enough to pass over them.
                var efDllFile = library.Value.GetProperty("files")
                    .EnumerateArray()
                    .Select(file => file.GetString()!)
                    .FirstOrDefault(file => file.EndsWith("/any/ef.dll", StringComparison.OrdinalIgnoreCase));

                if (efDllFile == null)
                {
                    throw new InvalidOperationException(
                        $"Could not locate ef.dll. The package '{library.Name}' does not contain one.");
                }

                return (library.Value.GetProperty("path").GetString()!, efDllFile);
            }
        }

        throw new InvalidOperationException(
            $"Could not locate ef.dll. The project does not reference the package " +
            $"'{EntityFrameworkPackageUtils.ToolsPackageId}'.");
    }
}
