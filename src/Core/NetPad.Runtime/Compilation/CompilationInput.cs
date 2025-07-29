using Microsoft.CodeAnalysis;
using NetPad.DotNet;

namespace NetPad.Compilation;

/// <summary>
/// The input that the <see cref="ICodeCompiler"/> uses to compile a script.
/// </summary>
public class CompilationInput(
    string code,
    DotNetFrameworkVersion targetFrameworkVersion,
    HashSet<byte[]>? assemblyImageReferences = null,
    HashSet<string>? assemblyFileReferences = null)
{
    /// <summary>
    /// The kind of assembly that was emitted as a result of compiling the script.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default to <see cref="OutputKind.ConsoleApplication"/> instead of <see cref="OutputKind.DynamicallyLinkedLibrary"/>
    /// so that the generated assembly can be run as an executable (ie. dotnet ./assembly.dll).
    /// </para>
    /// <para>
    /// Using <see cref="OutputKind.DynamicallyLinkedLibrary"/> generates an assembly that does not have an entry point,
    /// resulting in failure to execute assembly as a standalone external process.
    /// </para>
    /// </remarks>
    public OutputKind OutputKind { get; private set; } = OutputKind.ConsoleApplication;

    /// <summary>
    /// The target <see cref="DotNetFrameworkVersion"/> to compile the script for.
    /// </summary>
    public DotNetFrameworkVersion TargetFrameworkVersion { get; private set; } = targetFrameworkVersion;

    /// <summary>
    /// The code to compile.
    /// </summary>
    public string Code { get; } = code;

    /// <summary>
    /// The name to set for <see cref="System.Reflection.AssemblyName"/> in the assembly that will be emitted as a
    /// result of the compilation.
    /// </summary>
    public string? AssemblyName { get; private set; }

    /// <summary>
    /// The <see cref="OptimizationLevel"/> to use when compiling.
    /// </summary>
    public OptimizationLevel OptimizationLevel { get; private set; } = OptimizationLevel.Debug;

    /// <summary>
    /// Whether to include ASP.NET Core assembly references in the compilation.
    /// </summary>
    public bool UseAspNet { get; private set; }

    /// <summary>
    /// In-memory assemblies the code references and needs to compile.
    /// </summary>
    public HashSet<byte[]> AssemblyImageReferences { get; } = assemblyImageReferences ?? [];

    /// <summary>
    /// File paths of assemblies the code references and needs to compile.
    /// </summary>
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
