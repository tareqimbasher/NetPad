using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using NetPad.CodeAnalysis;
using NetPad.DotNet;
using NetPad.IO;

namespace NetPad.Compilation.CSharp;

public class CSharpCodeCompiler(IDotNetInfo dotNetInfo, ICodeAnalysisService codeAnalysisService)
    : ICodeCompiler
{
    public CompilationResult Compile(CompilationInput input)
    {
        string assemblyName = !string.IsNullOrWhiteSpace(input.AssemblyName) ? input.AssemblyName : "NetPadScript";

        string assemblyFileName = $"{assemblyName}{GetCompiledFileExtension(input.OutputKind)}";

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

        // Build references
        var assemblyLocations = FrameworkAssemblies.GetAssemblyLocations(
            dotNetInfo.LocateDotNetRootDirectoryOrThrow(),
            input.TargetFrameworkVersion,
            input.UseAspNet);

        foreach (var assemblyReferenceLocation in input.AssemblyFileReferences)
        {
            assemblyLocations.Add(assemblyReferenceLocation);
        }

        assemblyLocations.Add(typeof(IOutputWriter<>).Assembly.Location);

        var references = BuildMetadataReferences(input.AssemblyImageReferences, assemblyLocations);

        var analyzers = BuildAnalyzerReferences(assemblyLocations);
        var generators = analyzers.SelectMany(x => x.GetGeneratorsForAllLanguages()).ToArray();

        var compilationOptions = new CSharpCompilationOptions(input.OutputKind)
            .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
            .WithOptimizationLevel(input.OptimizationLevel)
            .WithOverflowChecks(true)
            .WithAllowUnsafe(true);

        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: new[] { syntaxTree },
            options: compilationOptions,
            references: references
        );

        // Now run the built-in generator infrastructure
        var driver = CSharpGeneratorDriver.Create(
            generators: generators,
            additionalTexts: null,
            parseOptions: (CSharpParseOptions)compilation.SyntaxTrees.First().Options,
            optionsProvider: null);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var sourceGenCompilation, out var diagnostics);
        return (CSharpCompilation)sourceGenCompilation;
    }

    private AnalyzerFileReference[] BuildAnalyzerReferences(
        HashSet<string> assemblyLocations)
    {
        return assemblyLocations
            .Select(loc => new AnalyzerFileReference(loc, new FromAssemblyLoader()))
            .ToArray();
    }

    private PortableExecutableReference[] BuildMetadataReferences(
        IEnumerable<byte[]> assemblyImages,
        HashSet<string> assemblyLocations)
    {
        return assemblyImages
            .Select(i => MetadataReference.CreateFromImage(i))
            .Union(assemblyLocations.Select(loc => MetadataReference.CreateFromFile(loc)))
            .ToArray();
    }

    private static string GetCompiledFileExtension(OutputKind outputKind)
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

    public void AddDependencyLocation(string fullPath) { }

    public Assembly LoadFromPath(string fullPath)
    {
        return _loaded.GetOrAdd(fullPath, Assembly.LoadFrom);
    }
}
