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
        string assemblyName = "NetPad_CompiledAssembly";

        if (input.OutputAssemblyNameTag != null)
            assemblyName += $"_{input.OutputAssemblyNameTag}";

        assemblyName = $"{assemblyName}_{Guid.NewGuid()}";

        var compilation = CreateCompilation(input, assemblyName);

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded));

        stream.Seek(0, SeekOrigin.Begin);
        return new CompilationResult(result.Success, new AssemblyName(assemblyName), assemblyName + ".dll", stream.ToArray(), result.Diagnostics);
    }

    private CSharpCompilation CreateCompilation(CompilationInput input, string assemblyName)
    {
        // Parse code
        SourceText sourceCode = SourceText.From(input.Code);

        CSharpParseOptions parseOptions = GetParseOptions(input.TargetFrameworkVersion);
        SyntaxTree parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(sourceCode, parseOptions);

        // Build references
        var assemblyLocations = SystemAssemblies.GetAssemblyLocations(_dotNetInfo, input.TargetFrameworkVersion);

        foreach (var assemblyReferenceLocation in input.AssemblyFileReferences)
            assemblyLocations.Add(assemblyReferenceLocation);

        assemblyLocations.Add(typeof(IOutputWriter<>).Assembly.Location);

        var references = BuildMetadataReferences(input.AssemblyImageReferences, assemblyLocations);

        var compilationOptions = new CSharpCompilationOptions(input.OutputKind)
            .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
            .WithOptimizationLevel(OptimizationLevel.Debug)
            .WithOverflowChecks(true)
            .WithAllowUnsafe(true);

        return CSharpCompilation.Create(assemblyName,
            new[] { parsedSyntaxTree },
            references: references,
            options: compilationOptions);
    }

    private PortableExecutableReference[] BuildMetadataReferences(IEnumerable<byte[]> assemblyImages, HashSet<string> assemblyLocations)
    {
        var references = assemblyImages.Select(i => MetadataReference.CreateFromImage(i)).ToList();

        // var locationReferences = assemblyLocations
        //     .Where(al => !string.IsNullOrWhiteSpace(al))
        //     .Select(location => new
        //     {
        //         MetadataReference = MetadataReference.CreateFromFile(location),
        //         AssemblyName = AssemblyName.GetAssemblyName(location)
        //     })
        //     .ToList();
        //
        // var duplicateReferences = locationReferences.GroupBy(r => r.AssemblyName.Name)
        //     .Where(grp => grp.Key != null && grp.Count() > 1);
        //
        // foreach (var duplicateReferenceGroup in duplicateReferences)
        // {
        //     // Take the highest version. If multiple of the same version, just take one.
        //     var duplicatesToRemove = duplicateReferenceGroup
        //         .GroupBy(x => x.AssemblyName.Version)
        //         .ToArray();
        //
        //     foreach (var duplicate in duplicatesToRemove)
        //     {
        //         foreach (var x in duplicate.Skip(1))
        //         {
        //             locationReferences.Remove(x);
        //         }
        //     }
        // }
        //
        // references.AddRange(locationReferences.Select(r => r.MetadataReference));

        references.AddRange(assemblyLocations.Select(location => MetadataReference.CreateFromFile(location)));

        return references.ToArray();
    }

    public CSharpParseOptions GetParseOptions(DotNetFrameworkVersion targetFrameworkVersion)
    {
        // TODO investigate using SourceKind.Script
        return CSharpParseOptions.Default
            .WithLanguageVersion(GetCSharpLanguageVersion(targetFrameworkVersion))
            .WithKind(SourceCodeKind.Regular);
    }

    private LanguageVersion GetCSharpLanguageVersion(DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        return dotNetFrameworkVersion switch
        {
            DotNetFrameworkVersion.DotNet6 => LanguageVersion.CSharp10,
            DotNetFrameworkVersion.DotNet7 => LanguageVersion.CSharp11,
            _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unhandled .NET framework version")
        };
    }
}
