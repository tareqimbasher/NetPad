using System.Collections.Concurrent;
using System.IO;
using NetPad.DotNet;
using NetPad.IO;

namespace NetPad.Compilation;

/// <summary>
/// A utility to get assemblies of the .NET framework itself.
/// </summary>
public static class FrameworkAssemblies
{
    private record CacheKey(DotNetFrameworkVersion DotNetFrameworkVersion, bool IncludeAspNet);

    private static readonly ConcurrentDictionary<CacheKey, HashSet<string>> _systemAssembliesLocations = new();

    /// <summary>
    /// Gets the assembly locations of the specified .NET framework version.
    /// </summary>
    /// <param name="dotNetRootDir">Where .NET is located (installed) on the local machine.</param>
    /// <param name="dotNetFrameworkVersion">The .NET framework version to target.</param>
    /// <param name="includeAspNet">Whether to include ASP.NET assemblies or not.</param>
    public static HashSet<string> GetAssemblyLocations(
        DirectoryPath dotNetRootDir,
        DotNetFrameworkVersion dotNetFrameworkVersion,
        bool includeAspNet
    )
    {
        var key = new CacheKey(dotNetFrameworkVersion, includeAspNet);

        return _systemAssembliesLocations.GetOrAdd(
                key,
                static (k, dni) =>
                    GetReferenceAssemblyLocationsFromDotNetRoot(dni, k.DotNetFrameworkVersion, k.IncludeAspNet),
                dotNetRootDir
            )
            .ToHashSet();
    }

    private static HashSet<string> GetImplementationAssemblyLocationsFromAppDomain()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly =>
                !assembly.IsDynamic &&
                !string.IsNullOrWhiteSpace(assembly.Location) &&
                assembly.GetName().Name?.StartsWith("System.") == true)
            .Select(assembly => assembly.Location)
            .ToHashSet();
    }

    private static HashSet<string> GetImplementationAssemblyLocationsFromAppContext()
    {
        string? assemblyPaths = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (string.IsNullOrWhiteSpace(assemblyPaths))
            throw new Exception("TRUSTED_PLATFORM_ASSEMBLIES is empty. " +
                                "Make sure you are not running the app as a Single File application.");

        var includeList = new[] { "System.", "mscorlib.", "netstandard." };

        return assemblyPaths
            .Split(Path.PathSeparator)
            .Where(path =>
            {
                var fileName = Path.GetFileName(path);
                return includeList.Any(p => fileName.StartsWith(p));
            })
            .ToHashSet();
    }

    private static HashSet<string> GetReferenceAssemblyLocationsFromDotNetRoot(
        DirectoryPath dotNetRootDir,
        DotNetFrameworkVersion dotNetFrameworkVersion,
        bool includeAspNet)
    {
        var assemblyDirectories =
            GetReferenceAssemblyDirectories(dotNetRootDir.Path, dotNetFrameworkVersion, includeAspNet);

        if (assemblyDirectories == null)
        {
            var implementationAssemblyDir =
                GetImplementationAssemblyDirectory(dotNetRootDir.Path, dotNetFrameworkVersion);
            if (implementationAssemblyDir != null)
            {
                assemblyDirectories = [implementationAssemblyDir];
            }
        }

        if (assemblyDirectories?.Any() != true)
        {
            throw new Exception(
                $"Could not locate .NET {dotNetFrameworkVersion.GetMajorVersion()} SDK reference or implementation assemblies using .NET SDK root: {dotNetRootDir.Path}");
        }

        return assemblyDirectories
            .SelectMany(d => Directory.GetFiles(d, "*.dll"))
            .Where(filePath => !filePath.Contains("VisualBasic"))
            .ToHashSet();
    }

    /// <summary>
    /// Gets paths of reference assemblies for a particular .NET version. Reference assemblies contain no actual implementation
    /// and only contain metadata.
    /// </summary>
    /// <param name="dotnetRoot">The absolute directory path where .NET SDK is installed.</param>
    /// <param name="dotNetFrameworkVersion">The .NET version.</param>
    /// <param name="includeAspNet">Whether to include ASP.NET Core reference assembly directories.</param>
    private static List<string>? GetReferenceAssemblyDirectories(string dotnetRoot,
        DotNetFrameworkVersion dotNetFrameworkVersion, bool includeAspNet)
    {
        var directories = new List<string>();
        var majorVersion = dotNetFrameworkVersion.GetMajorVersion();

        if (!AddDir("Microsoft.NETCore.App.Ref"))
        {
            return null;
        }

        if (includeAspNet && !AddDir("Microsoft.AspNetCore.App.Ref"))
        {
            return null;
        }

        return directories;

        bool AddDir(string packName)
        {
            var referenceAssemblyRoot = new DirectoryInfo(Path.Combine(dotnetRoot, "packs", packName));

            var latestMinorVersionDir = GetLatestVersionDir(referenceAssemblyRoot, majorVersion)?.Name;

            if (latestMinorVersionDir != null)
            {
                var target = Path.Combine(
                    referenceAssemblyRoot.FullName,
                    latestMinorVersionDir,
                    "ref",
                    $"net{majorVersion}.0");

                if (!Directory.Exists(target))
                {
                    return false;
                }

                directories.Add(target);

                var analysers = Path.Combine(
                    referenceAssemblyRoot.FullName,
                    latestMinorVersionDir,
                    "analyzers",
                    "dotnet",
                    "cs");

                if (Directory.Exists(analysers))
                {
                    directories.Add(analysers);
                }
            }
            else
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Gets paths of implementation assemblies for a particular .NET version. Reference assemblies should be preferred over
    /// implementation assemblies to compile against.
    /// </summary>
    /// <param name="dotnetRoot">The absolute directory path where .NET SDK is installed.</param>
    /// <param name="dotNetFrameworkVersion">The .NET version.</param>
    private static string? GetImplementationAssemblyDirectory(string dotnetRoot,
        DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        var runtimeImplementationAssemblyRoot =
            new DirectoryInfo(Path.Combine(dotnetRoot, "shared", "Microsoft.NETCore.App"));

        var latestApplicableDirName =
            GetLatestVersionDir(runtimeImplementationAssemblyRoot, dotNetFrameworkVersion.GetMajorVersion())?.Name;

        if (latestApplicableDirName == null)
        {
            return null;
        }

        var implementationAssembliesDir =
            Path.Combine(runtimeImplementationAssemblyRoot.FullName, latestApplicableDirName);

        return !Directory.Exists(implementationAssembliesDir) ? null : implementationAssembliesDir;
    }

    private static DirectoryInfo? GetLatestVersionDir(DirectoryInfo root, int targetMajorVersion)
    {
        if (!root.Exists)
        {
            return null;
        }

        return root.GetDirectories()
            .Select(d => SemanticVersion.TryParse(d.Name, out var version)
                ? new { Directory = d, Version = version }
                : null)
            .Where(d => d != null && d.Version.Major == targetMajorVersion)
            .MaxBy(d => d!.Version)?
            .Directory;
    }
}
