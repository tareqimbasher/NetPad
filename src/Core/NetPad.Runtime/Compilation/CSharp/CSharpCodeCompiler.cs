using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
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
    private static readonly ConcurrentDictionary<(string Path, long LastWriteTicks), PortableExecutableReference>
        _metadataReferenceCache = new();

    private static readonly IAnalyzerAssemblyLoader _sourceGeneratorAssemblyLoader =
        new SourceGeneratorAssemblyLoader();

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
        var dotNetRoot = dotNetInfo.LocateDotNetRootDirectoryForFramework(input.TargetFrameworkVersion)
                         ?? dotNetInfo.LocateDotNetRootDirectoryOrThrow();
        var assemblyLocations = FrameworkAssemblies.GetAssemblyLocations(
            dotNetRoot,
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
            .Union(assemblyLocations.Select(loc => _metadataReferenceCache.GetOrAdd(
                (loc, File.GetLastWriteTimeUtc(loc).Ticks),
                key => MetadataReference.CreateFromFile(key.Path))
            ))
            .ToArray();
    }

    private static ISourceGenerator[] GetSourceGenerators(HashSet<string> assemblyLocations)
    {
        return assemblyLocations
            .Select(loc => new AnalyzerFileReference(loc, _sourceGeneratorAssemblyLoader))
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

/// <summary>
/// Loads source generator assemblies, giving each directory its own load context.
/// </summary>
/// <remarks>
/// Every .NET version ships generators with the same assembly names, for example
/// System.Text.RegularExpressions.Generator, and a load context can hold only one assembly per name. With a single
/// context, only the first .NET version compiled would get its generators.
/// </remarks>
file class SourceGeneratorAssemblyLoader : IAnalyzerAssemblyLoader
{
    private readonly object _lock = new();
    private readonly Dictionary<string, Assembly> _loaded = new();
    private readonly Dictionary<string, DirectoryLoadContext> _contexts = new();

    public void AddDependencyLocation(string fullPath)
    {
    }

    public Assembly LoadFromPath(string fullPath)
    {
        lock (_lock)
        {
            if (_loaded.TryGetValue(fullPath, out var assembly))
            {
                return assembly;
            }

            var directory = Path.GetDirectoryName(fullPath)!;
            if (!_contexts.TryGetValue(directory, out var context))
            {
                context = new DirectoryLoadContext(directory);
                _contexts[directory] = context;
            }

            assembly = context.LoadFromAssemblyPath(fullPath);
            _loaded[fullPath] = assembly;
            return assembly;
        }
    }
}

file class DirectoryLoadContext(string directory) : AssemblyLoadContext($"Source generators: {directory}")
{
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Use NetPad's own copy of an assembly when it has one. A generator must share the compiler that runs it, so it
        // can't load a separate copy of the compiler from its own folder.
        try
        {
            return Default.LoadFromAssemblyName(assemblyName);
        }
        catch (IOException)
        {
            var path = Path.Combine(directory, $"{assemblyName.Name}.dll");
            return File.Exists(path) ? LoadFromAssemblyPath(path) : null;
        }
    }
}
