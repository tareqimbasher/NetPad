using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace NetPad.Compilation;

public class CompilationResult(
    bool success,
    AssemblyName assemblyName,
    string assemblyFileName,
    byte[] assemblyBytes,
    ImmutableArray<Diagnostic> diagnostics)
{
    public bool Success { get; } = success;
    public AssemblyName AssemblyName { get; } = assemblyName;
    public string AssemblyFileName { get; } = assemblyFileName;
    public byte[] AssemblyBytes { get; } = assemblyBytes;
    public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
}
