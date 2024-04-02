using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using NetPad.DotNet;
using NetPad.IO;

namespace NetPad.Compilation.CSharp;

public class CSharpCodeCompiler : ICodeCompiler
{
    private readonly IDotNetInfo _dotNetInfo;

    public CSharpCodeCompiler(IDotNetInfo dotNetInfo)
    {
        _dotNetInfo = dotNetInfo;
    }

    public CompilationResult Compile(CompilationInput input)
    {
        string assemblyName = !string.IsNullOrWhiteSpace(input.AssemblyName) ? input.AssemblyName : "NetPadScript";

        string assemblyFileName = $"{assemblyName}{GetCompiledFileExtension(input.OutputKind)}";

        var compilation = CreateCompilation(input, assemblyName);

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded));

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
        // Parse code
        SourceText sourceCode = SourceText.From(input.Code);
        CSharpParseOptions parseOptions = GetParseOptions(input.TargetFrameworkVersion, input.OptimizationLevel);
        SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(sourceCode, parseOptions);

        // Build references
        var assemblyLocations = SystemAssemblies.GetAssemblyLocations(_dotNetInfo, input.TargetFrameworkVersion, input.UseAspNet);

        foreach (var assemblyReferenceLocation in input.AssemblyFileReferences)
            assemblyLocations.Add(assemblyReferenceLocation);

        assemblyLocations.Add(typeof(IOutputWriter<>).Assembly.Location);

        var references = BuildMetadataReferences(input.AssemblyImageReferences, assemblyLocations);

        var compilationOptions = new CSharpCompilationOptions(input.OutputKind)
            .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
            .WithOptimizationLevel(input.OptimizationLevel)
            .WithOverflowChecks(true)
            .WithAllowUnsafe(true);

        return CSharpCompilation.Create(assemblyName,
            new[] { syntaxTree },
            references: references,
            options: compilationOptions);
    }

    private PortableExecutableReference[] BuildMetadataReferences(IEnumerable<byte[]> assemblyImages, HashSet<string> assemblyLocations)
    {
        var references = assemblyImages.Select(i => MetadataReference.CreateFromImage(i)).ToList();

        references.AddRange(assemblyLocations.Select(location => MetadataReference.CreateFromFile(location)));

        return references.ToArray();
    }

    public CSharpParseOptions GetParseOptions(DotNetFrameworkVersion targetFrameworkVersion, OptimizationLevel optimizationLevel)
    {
        return CSharpParseOptions.Default
            .WithLanguageVersion(DotNetFrameworkVersionUtil.GetLatestSupportedCSharpLanguageVersion(targetFrameworkVersion))
            // TODO investigate using SourceKind.Script (see cs-scripts branch)
            .WithKind(SourceCodeKind.Regular)
            .WithPreprocessorSymbols(PreprocessorSymbols.For(optimizationLevel));
    }

    private static string GetCompiledFileExtension(OutputKind outputKind)
    {
        return outputKind switch
        {
            OutputKind.DynamicallyLinkedLibrary => ".dll",
            OutputKind.ConsoleApplication => ExeExtension(),
            OutputKind.WindowsApplication => ExeExtension(),
            OutputKind.WindowsRuntimeMetadata => ExeExtension(),
            OutputKind.WindowsRuntimeApplication => ".winmdobj",
            OutputKind.NetModule => ".netmodule",
            _ => throw new ArgumentOutOfRangeException(nameof(outputKind), outputKind, null)

        };

        static string ExeExtension () => PlatformUtil.IsWindowsPlatform() ? ".exe" : string.Empty;
    }
}
