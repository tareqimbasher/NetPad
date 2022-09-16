using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using NetPad.IO;

namespace NetPad.Compilation.CSharp
{
    public class CSharpCodeCompiler : ICodeCompiler
    {
        public CompilationResult Compile(CompilationInput input)
        {
            // TODO write a unit test to test assembly name
            string assemblyName = "NetPad_CompiledAssembly";

            if (input.OutputAssemblyNameTag != null)
                assemblyName += $"_{input.OutputAssemblyNameTag}";

            assemblyName = $"{assemblyName}_{Guid.NewGuid()}.dll";

            var compilation = CreateCompilation(input, assemblyName);

            using var stream = new MemoryStream();
            var result = compilation.Emit(stream);

            stream.Seek(0, SeekOrigin.Begin);
            return new CompilationResult(result.Success,  assemblyName, stream.ToArray(), result.Diagnostics);
        }

        private CSharpCompilation CreateCompilation(CompilationInput input, string assemblyName)
        {
            // Parse code
            SourceText sourceCode = SourceText.From(input.Code);

            CSharpParseOptions parseOptions = GetParseOptions();
            SyntaxTree parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(sourceCode, parseOptions);

            // Build references
            var assemblyLocations = SystemAssemblies.GetAssemblyLocations();

            foreach (var assemblyReferenceLocation in input.AssemblyReferenceLocations)
                assemblyLocations.Add(assemblyReferenceLocation);

            assemblyLocations.Add(typeof(IOutputWriter).Assembly.Location);

            var references = BuildMetadataReferences(assemblyLocations);

            var compilationOptions = new CSharpCompilationOptions(input.OutputKind)
                .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                .WithOptimizationLevel(OptimizationLevel.Debug)
                .WithOverflowChecks(true);

            return CSharpCompilation.Create(assemblyName,
                new[] { parsedSyntaxTree },
                references: references,
                options: compilationOptions);
        }

        private PortableExecutableReference[] BuildMetadataReferences(HashSet<string> assemblyLocations)
        {
            var references = assemblyLocations
                .Where(al => !string.IsNullOrWhiteSpace(al))
                .Select(location => new
                {
                    MetadataReference = MetadataReference.CreateFromFile(location),
                    AssemblyName = AssemblyName.GetAssemblyName(location)
                })
                .ToList();

            var duplicateReferences = references.GroupBy(r => r.AssemblyName.Name)
                .Where(grp => grp.Key != null && grp.Count() > 1);

            foreach (var duplicateReferenceGroup in duplicateReferences)
            {
                // Take the lowest version. If multiple of the same version, just take one.
                var duplicatesToRemove = duplicateReferenceGroup
                    .OrderBy(x => x.AssemblyName.Version)
                    .Skip(1)
                    .ToArray();

                foreach (var duplicate in duplicatesToRemove)
                {
                    references.Remove(duplicate);
                }
            }

            return references.Select(r => r.MetadataReference).ToArray();
        }

        public CSharpParseOptions GetParseOptions()
        {
            // TODO investigate using SourceKind.Script
            return CSharpParseOptions.Default
                .WithLanguageVersion(LanguageVersion.CSharp10)
                .WithKind(SourceCodeKind.Regular);
        }
    }
}
