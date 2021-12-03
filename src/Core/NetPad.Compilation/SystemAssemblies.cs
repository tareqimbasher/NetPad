using System;
using System.Collections.Generic;
using System.Linq;

namespace NetPad.Compilation
{
    public static class SystemAssemblies
    {
        private static HashSet<string>? _systemAssembliesLocations;

        public static HashSet<string> GetAssemblyLocations()
        {
            if (_systemAssembliesLocations == null)
            {
                _systemAssembliesLocations = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(assembly =>
                        !assembly.IsDynamic &&
                        !string.IsNullOrWhiteSpace(assembly.Location) &&
                        assembly.GetName()?.Name?.StartsWith("System.") == true)
                    .Select(assembly => assembly.Location)
                    .ToHashSet();
            }

            return _systemAssembliesLocations;
        }
    }
}
