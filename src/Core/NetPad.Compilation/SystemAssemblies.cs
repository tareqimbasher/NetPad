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
            return _systemAssembliesLocations ??= GetAssemblyLocationsFromAppContext();
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
    }
}
