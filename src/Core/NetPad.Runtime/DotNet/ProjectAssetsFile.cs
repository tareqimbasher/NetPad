using System.IO;
using System.Text.Json;

namespace NetPad.DotNet;

/// <summary>
/// What a restore recorded in a project's <c>project.assets.json</c> file.
/// </summary>
/// <param name="PackageFolders">Every folder the project's packages were restored to or found in.</param>
/// <param name="Libraries">The packages (and projects) the restore resolved, one per id.</param>
public sealed record ProjectAssetsFile(IReadOnlyList<string> PackageFolders, IReadOnlyList<ProjectAssetsLibrary> Libraries)
{
    /// <summary>
    /// Reads a project's <c>project.assets.json</c> file.
    /// </summary>
    /// <param name="assetsFilePath">The path to the <c>project.assets.json</c> file.</param>
    /// <exception cref="InvalidOperationException">Thrown when the file does not exist.</exception>
    public static ProjectAssetsFile Read(string assetsFilePath)
    {
        if (!File.Exists(assetsFilePath))
        {
            throw new InvalidOperationException(
                $"The project has not been restored: '{assetsFilePath}' does not exist.");
        }

        return Parse(File.ReadAllText(assetsFilePath));
    }

    /// <summary>
    /// Reads the contents of a <c>project.assets.json</c> file.
    /// </summary>
    /// <param name="assetsJson">The contents of the file.</param>
    public static ProjectAssetsFile Parse(string assetsJson)
    {
        using var document = JsonDocument.Parse(assetsJson);
        var root = document.RootElement;

        var packageFolders = root.TryGetProperty("packageFolders", out var folders)
            ? folders.EnumerateObject().Select(folder => folder.Name).ToArray()
            : [];

        var libraries = root.TryGetProperty("libraries", out var librariesElement)
            ? librariesElement.EnumerateObject().Select(ParseLibrary).OfType<ProjectAssetsLibrary>().ToArray()
            : [];

        return new ProjectAssetsFile(packageFolders, libraries);
    }

    /// <summary>
    /// Finds the library the restore resolved for a package.
    /// </summary>
    /// <param name="packageId">The package ID.</param>
    /// <returns>The library, or <c>null</c> if the restore did not resolve the package.</returns>
    public ProjectAssetsLibrary? FindPackage(string packageId)
    {
        return Libraries.FirstOrDefault(library => library.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the version of a package that the restore resolved, whether the project references the package directly
    /// or gets it through another package.
    /// </summary>
    /// <param name="packageId">The package ID.</param>
    /// <exception cref="InvalidOperationException">Thrown when the restore did not resolve the package.</exception>
    public string GetPackageVersion(string packageId)
    {
        return FindPackage(packageId)?.Version
               ?? throw new InvalidOperationException(
                   $"Could not determine which version of '{packageId}' the project uses. The project's restore " +
                   $"did not resolve that package.");
    }

    private static ProjectAssetsLibrary? ParseLibrary(JsonProperty library)
    {
        // Libraries are keyed as "{id}/{version}"
        var separatorIndex = library.Name.IndexOf('/');
        if (separatorIndex <= 0)
        {
            return null;
        }

        var path = library.Value.TryGetProperty("path", out var pathElement) ? pathElement.GetString() : null;

        var files = library.Value.TryGetProperty("files", out var filesElement)
            ? filesElement.EnumerateArray().Select(file => file.GetString()!).ToArray()
            : [];

        return new ProjectAssetsLibrary(
            library.Name[..separatorIndex],
            library.Name[(separatorIndex + 1)..],
            path,
            files);
    }
}

/// <summary>
/// A package (or project) that a restore resolved, as recorded in <c>project.assets.json</c>.
/// </summary>
/// <param name="Id">The package ID.</param>
/// <param name="Version">The resolved version.</param>
/// <param name="Path">The package's folder, relative to a package folder. <c>null</c> for a project.</param>
/// <param name="Files">The package's files, relative to its folder.</param>
public sealed record ProjectAssetsLibrary(string Id, string Version, string? Path, IReadOnlyList<string> Files);
