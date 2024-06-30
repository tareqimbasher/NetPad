using Microsoft.CodeAnalysis;
using NetPad.DotNet;

namespace NetPad.Compilation;

public class CompilationInput(
    string code,
    DotNetFrameworkVersion targetFrameworkVersion,
    HashSet<byte[]>? assemblyImageReferences = null,
    HashSet<string>? assemblyFileReferences = null)
{
    // Default to OutputKind.ConsoleApplication instead OutputKind.DynamicallyLinkedLibrary so the generated assembly
    // is able to be executed as an executable (ie. dotnet ./assembly.dll). Using OutputKind.DynamicallyLinkedLibrary
    // generates an assembly that does not have an entry point, resulting in failure to execute standalone assembly
    // as an external process.
    // Another reason to use OutputKind.ConsoleApplication is we are using top-level statements, which we cannot
    // compile the assembly with if set to OutputKind.DynamicallyLinkedLibrary.

    public OutputKind OutputKind { get; private set; } = OutputKind.ConsoleApplication;
    public DotNetFrameworkVersion TargetFrameworkVersion { get; private set; } = targetFrameworkVersion;
    public string Code { get; } = code;
    public string? AssemblyName { get; private set; }
    public OptimizationLevel OptimizationLevel { get; private set; } = OptimizationLevel.Debug;
    public bool UseAspNet { get; private set; }
    public HashSet<byte[]> AssemblyImageReferences { get; } = assemblyImageReferences ?? [];
    public HashSet<string> AssemblyFileReferences { get; } = assemblyFileReferences ?? [];

    public CompilationInput WithOutputKind(OutputKind outputKind)
    {
        OutputKind = outputKind;
        return this;
    }

    public CompilationInput WithAssemblyName(string? assemblyName)
    {
        AssemblyName = assemblyName;
        return this;
    }

    public CompilationInput WithUseAspNet(bool useAspNet = true)
    {
        UseAspNet = useAspNet;
        return this;
    }

    public CompilationInput WithOptimizationLevel(OptimizationLevel level)
    {
        OptimizationLevel = level;
        return this;
    }
}
