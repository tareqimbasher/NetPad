using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace NetPad.Compilation
{
    public class CompilationResult
    {
        public CompilationResult(bool success, byte[] assemblyBytes, ImmutableArray<Diagnostic> diagnostics)
        {
            Success = success;
            AssemblyBytes = assemblyBytes;
            Diagnostics = diagnostics;
        }

        public bool Success { get; }
        public byte[] AssemblyBytes { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
    }
}
