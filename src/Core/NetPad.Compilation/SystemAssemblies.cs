using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetPad.DotNet;

namespace NetPad.Compilation
{
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
            var coreLibPath = typeof(object).Assembly.Location; // ex: /DOTNET_ROOT/shared/Microsoft.NETCore.App/6.0.7/System.Private.CoreLib.dll
            var dotnetVer = Path.GetFileName(Path.GetDirectoryName(coreLibPath));      // ex: 6.0.7
            if (dotnetVer == null)
                throw new Exception("Could not determine dotnet version");

            var dotnetRoot = DotNetInfo.LocateDotNetRootDirectoryOrThrow();
            var referenceAssembliesDir = Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref", dotnetVer, "ref", $"net{dotnetVer[0]}.0");

            return Directory.GetFiles(referenceAssembliesDir, "*.dll")
                .Where(a => !a.Contains("VisualBasic"))
                .ToHashSet();
        }
    }
}
