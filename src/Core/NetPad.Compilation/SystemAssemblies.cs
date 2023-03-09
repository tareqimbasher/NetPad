using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetPad.Common;
using NetPad.DotNet;

namespace NetPad.Compilation;

public static class SystemAssemblies
{
    private static HashSet<string>? _systemAssembliesLocations;

    public static HashSet<string> GetAssemblyLocations()
    {
        return _systemAssembliesLocations ??= GetReferenceAssemblyLocationsFromDotNetRoot();
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

    private static HashSet<string> GetReferenceAssemblyLocationsFromDotNetRoot()
    {
        var dotnetRoot = DotNetInfo.LocateDotNetRootDirectoryOrThrow();
        var sdkReferenceAssemblyRoot =
            new DirectoryInfo(Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref"));
        var dotnetVer = sdkReferenceAssemblyRoot.GetDirectories()
            .Select(d => Version.TryParse(d.Name, out var version)
                ? new
                {
                    Directory = d,
                    Version = version
                }
                : null)
            .Where(d => d != null)
            .MaxBy(d => d!.Version)?
            .Directory.Name;

        var referenceAssembliesDir =
            Path.Combine(sdkReferenceAssemblyRoot.FullName, dotnetVer, "ref", $"net{dotnetVer[0]}.0");

        if (!Directory.Exists(referenceAssembliesDir))
        {
            throw new Exception($"Could not find reference assemblies directory at {referenceAssembliesDir}." +
                                $" dotnetVer: {dotnetVer}." +
                                $" sdkReferenceAssemblyRoot: {sdkReferenceAssemblyRoot}");
        }

        return Directory.GetFiles(referenceAssembliesDir, "*.dll")
            .Where(a => !a.Contains("VisualBasic"))
            .ToHashSet();
    }
}
