using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace NetPad.Compilation
{
    public class CompilationResult
    {
        public CompilationResult(bool success, string assemblyName, byte[] assemblyBytes, ImmutableArray<Diagnostic> diagnostics)
        {
            Success = success;
            AssemblyName = assemblyName;
            AssemblyBytes = assemblyBytes;
            Diagnostics = diagnostics;
        }

        public bool Success { get; }
        public string AssemblyName { get; }
        public byte[] AssemblyBytes { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
    }
}
