using System;
using System.Collections.Generic;

namespace NetPad.Compilation
{
    public class CompilationInput
    {
        public string Code { get; }
        public IEnumerable<string> AssemblyReferenceLocations { get; }

        public CompilationInput(string code, IEnumerable<string>? assemblyReferenceLocations = null)
        {
            Code = code;
            AssemblyReferenceLocations = assemblyReferenceLocations ?? Array.Empty<string>();
        }
    }
}
