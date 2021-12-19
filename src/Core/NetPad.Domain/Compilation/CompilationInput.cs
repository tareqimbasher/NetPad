using System;
using System.Collections.Generic;

namespace NetPad.Compilation
{
    public class CompilationInput
    {
        public CompilationInput(string code, IEnumerable<string>? assemblyReferenceLocations = null)
        {
            Code = code;
            AssemblyReferenceLocations = assemblyReferenceLocations ?? Array.Empty<string>();
        }

        public string Code { get; }
        public string? OutputAssemblyNameTag { get; set; }
        public IEnumerable<string> AssemblyReferenceLocations { get; }
    }
}
