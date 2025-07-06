using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using NetPad.CodeAnalysis;
using NetPad.DotNet;

namespace NetPad.Compilation.CSharp;

/// <summary>
/// An implementation of <see cref="ICodeCompiler"/> that compiles C#.NET code.
/// </summary>
public class CSharpCodeCompiler(IDotNetInfo dotNetInfo, ICodeAnalysisService codeAnalysisService)
    : ICodeCompiler
{
    public CompilationResult Compile(CompilationInput input)
    {
        string assemblyName = !string.IsNullOrWhiteSpace(input.AssemblyName) ? input.AssemblyName : "NetPadScript";
        string assemblyFileExtension = GetCompiledAssemblyFileExtension(input.OutputKind);
        string assemblyFileName = $"{assemblyName}{assemblyFileExtension}";

        var compilation = CreateCompilation(input, assemblyName);

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream,
            options: new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded));

        stream.Seek(0, SeekOrigin.Begin);
        var assemblyBytes = stream.ToArray();

        return new CompilationResult(
            result.Success,
            new AssemblyName(assemblyName),
            assemblyFileName,
            assemblyBytes,
            result.Diagnostics);
    }

    private CSharpCompilation CreateCompilation(CompilationInput input, string assemblyName)
    {
        SyntaxTree syntaxTree = codeAnalysisService.GetSyntaxTree(
            input.Code,
            input.TargetFrameworkVersion,
            input.OptimizationLevel);

        // Build references to all assemblies we need to include in the compilation, starting with the assemblies
        // of the selected .NET SDK.
        var assemblyLocations = FrameworkAssemblies.GetAssemblyLocations(
            dotNetInfo.LocateDotNetRootDirectoryOrThrow(),
            input.TargetFrameworkVersion,
            input.UseAspNet);

        // Add assemblies coming from input
        foreach (var assemblyReferenceLocation in input.AssemblyFileReferences)
        {
            assemblyLocations.Add(assemblyReferenceLocation);
        }

        // Add a reference to the NetPad runtime
        assemblyLocations.Add(typeof(INetPadRuntimeLibMarker).Assembly.Location);

        // Convert locations to metadata references
        var references = BuildMetadataReferences(input.AssemblyImageReferences, assemblyLocations);

        var compilationOptions = new CSharpCompilationOptions(input.OutputKind)
            .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
            .WithOptimizationLevel(input.OptimizationLevel)
            .WithOverflowChecks(true)
            .WithAllowUnsafe(true);

        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: [syntaxTree],
            options: compilationOptions,
            references: references
        );

        // Run the built-in source generators
        var generators = GetSourceGenerators(assemblyLocations);
        var driver = CSharpGeneratorDriver.Create(
            generators: generators,
            additionalTexts: null,
            parseOptions: (CSharpParseOptions)compilation.SyntaxTrees[0].Options,
            optionsProvider: null);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var sourceGenCompilation, out _);
        return (CSharpCompilation)sourceGenCompilation;
    }

    private static PortableExecutableReference[] BuildMetadataReferences(
        IEnumerable<byte[]> assemblyImages,
        HashSet<string> assemblyLocations)
    {
        return assemblyImages
            .Select(i => MetadataReference.CreateFromImage(i))
            .Union(assemblyLocations.Select(loc => MetadataReference.CreateFromFile(loc)))
            .ToArray();
    }

    private static ISourceGenerator[] GetSourceGenerators(HashSet<string> assemblyLocations)
    {
        var assemblyLoader = new FromAssemblyLoader();
        return assemblyLocations
            .Select(loc => new AnalyzerFileReference(loc, assemblyLoader))
            .SelectMany(x => x.GetGenerators("C#"))
            .ToArray();
    }

    private static string GetCompiledAssemblyFileExtension(OutputKind outputKind)
    {
        var executableExt = PlatformUtil.GetPlatformExecutableExtension();

        return outputKind switch
        {
            OutputKind.DynamicallyLinkedLibrary => ".dll",
            OutputKind.ConsoleApplication => executableExt,
            OutputKind.WindowsApplication => executableExt,
            OutputKind.WindowsRuntimeMetadata => executableExt,
            OutputKind.WindowsRuntimeApplication => ".winmdobj",
            OutputKind.NetModule => ".netmodule",
            _ => throw new ArgumentOutOfRangeException(nameof(outputKind), outputKind, null)
        };
    }
}

file class FromAssemblyLoader : IAnalyzerAssemblyLoader
{
    private readonly ConcurrentDictionary<string, Assembly> _loaded = new();

    public void AddDependencyLocation(string fullPath)
    {
    }

    public Assembly LoadFromPath(string fullPath)
    {
        return _loaded.GetOrAdd(fullPath, Assembly.LoadFrom);
    }
}
