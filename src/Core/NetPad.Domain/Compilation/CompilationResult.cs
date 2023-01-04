using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace NetPad.Compilation;

public class CompilationResult
{
    public CompilationResult(bool success, AssemblyName assemblyName, string assemblyFileName, byte[] assemblyBytes, ImmutableArray<Diagnostic> diagnostics)
    {
        Success = success;
        AssemblyName = assemblyName;
        AssemblyFileName = assemblyFileName;
        AssemblyBytes = assemblyBytes;
        Diagnostics = diagnostics;
    }

    public bool Success { get; }
    public AssemblyName AssemblyName { get; }
    public string AssemblyFileName { get; }
    public byte[] AssemblyBytes { get; }
    public ImmutableArray<Diagnostic> Diagnostics { get; }
}
