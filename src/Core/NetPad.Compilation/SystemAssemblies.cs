using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NetPad.Compilation
{
    public static class SystemAssemblies
    {
        private static HashSet<string>? _systemAssembliesLocations;

        public static HashSet<string> GetAssemblyLocations()
        {
            return _systemAssembliesLocations ??= GetAssemblyLocationsFromDotNetRoot();
        }

        private static HashSet<string> GetAssemblyLocationsFromAppDomain()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly =>
                    !assembly.IsDynamic &&
                    !string.IsNullOrWhiteSpace(assembly.Location) &&
                    assembly.GetName().Name?.StartsWith("System.") == true)
                .Select(assembly => assembly.Location)
                .ToHashSet();
        }

        private static HashSet<string> GetAssemblyLocationsFromAppContext()
        {
            return (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)? // Empty in self-contained apps
                .Split(Path.PathSeparator)
                .Where(x =>
                {
                    var fileName = Path.GetFileName(x);
                    return !string.IsNullOrWhiteSpace(x) &&
                           new[] { "System.", "mscorlib.", "netstandard." }.Any(p => fileName.StartsWith(p));
                }).ToHashSet()
                ?? new HashSet<string>();
        }

        private static HashSet<string> GetAssemblyLocationsFromDotNetRoot()
        {
            var dotnetPath = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            if (string.IsNullOrWhiteSpace(dotnetPath) || !Directory.Exists(dotnetPath))
                throw new Exception($"DOTNET_ROOT variable must be set and directory must exist. Current value: {dotnetPath}");

            var netCoreDir = new DirectoryInfo(Path.Combine(dotnetPath, "shared/Microsoft.NETCore.App"));
            if (!netCoreDir.Exists)
                throw new Exception($"Required directory not found at {netCoreDir.FullName}");

            var dotnet5Dir = netCoreDir
                .GetDirectories()
                .Where(d => d.Name.StartsWith("5."))
                .OrderBy(d => d.Name)
                .Last();

            var locations = dotnet5Dir.GetFiles("*.dll")
                .Where(dll =>
                {
                    return new[] { "System.", "mscorlib.", "netstandard." }
                        .Any(p => dll.Name.StartsWith(p));
                })
                .Select(dll => dll.FullName);

            return locations.ToHashSet();
        }
    }
}
