using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetPad.DotNet;

namespace NetPad.Compilation;

public static class SystemAssemblies
{
    private static readonly ConcurrentDictionary<DotNetFrameworkVersion, HashSet<string>> _systemAssembliesLocations = new();

    public static HashSet<string> GetAssemblyLocations(IDotNetInfo dotNetInfo, DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        return _systemAssembliesLocations.GetOrAdd(
                dotNetFrameworkVersion, static (framework, dni) => GetReferenceAssemblyLocationsFromDotNetRoot(dni, framework), dotNetInfo)
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

    private static HashSet<string> GetReferenceAssemblyLocationsFromDotNetRoot(IDotNetInfo dotNetInfo, DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        var targetMajorVersion = dotNetFrameworkVersion.GetMajorVersion();

        var dotnetRoot = dotNetInfo.LocateDotNetRootDirectoryOrThrow();

        var assembliesDirectory = GetReferenceAssembliesDirectory(dotnetRoot, targetMajorVersion)
                                  ?? GetImplementationAssembliesDirectory(dotnetRoot, targetMajorVersion);

        if (assembliesDirectory == null)
        {
            throw new Exception($".NET {targetMajorVersion} SDK could not be found.");
        }

        return Directory.GetFiles(assembliesDirectory, "*.dll")
            .Where(a => !a.Contains("VisualBasic"))
            .ToHashSet();
    }

    private static string? GetReferenceAssembliesDirectory(string dotnetRoot, int targetMajorVersion)
    {
        var sdkReferenceAssemblyRoot = new DirectoryInfo(Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref"));

        var latestVersionDir = GetLatestVersionDir(sdkReferenceAssemblyRoot, targetMajorVersion)?.Name;

        if (latestVersionDir == null)
        {
            return null;
        }

        var referenceAssembliesDir = Path.Combine(sdkReferenceAssemblyRoot.FullName, latestVersionDir, "ref", $"net{targetMajorVersion}.0");

        return !Directory.Exists(referenceAssembliesDir) ? null : referenceAssembliesDir;
    }

    private static string? GetImplementationAssembliesDirectory(string dotnetRoot, int targetMajorVersion)
    {
        var runtimeImplementationAssemblyRoot = new DirectoryInfo(Path.Combine(dotnetRoot, "shared", "Microsoft.NETCore.App"));

        var latestApplicableDirName = GetLatestVersionDir(runtimeImplementationAssemblyRoot, targetMajorVersion)?.Name;

        if (latestApplicableDirName == null)
        {
            return null;
        }

        var implementationAssembliesDir = Path.Combine(runtimeImplementationAssemblyRoot.FullName, latestApplicableDirName);

        return !Directory.Exists(implementationAssembliesDir) ? null : implementationAssembliesDir;
    }

    private static DirectoryInfo? GetLatestVersionDir(DirectoryInfo directory, int targetMajorVersion)
    {
        if (!directory.Exists)
        {
            return null;
        }

        return directory.GetDirectories()
            .Select(d => Version.TryParse(d.Name, out var version)
                ? new { Directory = d, Version = version }
                : null)
            .Where(d => d != null && d.Version.Major == targetMajorVersion)
            .MaxBy(d => d!.Version)?
            .Directory;
    }
}
