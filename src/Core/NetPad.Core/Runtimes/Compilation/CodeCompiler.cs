using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using NetPad.Exceptions;

namespace NetPad.Runtimes.Compilation
{
    public class CodeCompiler
    {
        public byte[] Compile(CompilationInput input)
        {
            var compilation = CreateCompilation(input);

            using var stream = new MemoryStream();
            var result = compilation.Emit(stream);

            if (!result.Success)
            {
                throw new CodeCompilationException("Code compilation failed.", result.Diagnostics);
            }

            stream.Seek(0, SeekOrigin.Begin);
            return stream.ToArray();
        }

        public CSharpCompilation CreateCompilation(CompilationInput input)
        {
            // Parse code
            var sourceCode = SourceText.From(input.Code);

            var parseOptions = CSharpParseOptions.Default
                .WithLanguageVersion(LanguageVersion.CSharp9)
                .WithKind(SourceCodeKind.Regular);
            var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(sourceCode, parseOptions);

            // Build references
            var assemblyLocations = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly =>
                    !assembly.IsDynamic &&
                    !string.IsNullOrWhiteSpace(assembly.Location) &&
                    assembly.GetName()?.Name?.StartsWith("System.") == true)
                .Select(assembly => assembly.Location)
                .ToHashSet();

            foreach (var assemblyReferenceLocation in input.AssemblyReferenceLocations)
                assemblyLocations.Add(assemblyReferenceLocation);

            assemblyLocations.Add(typeof(IQueryRuntimeOutputWriter).Assembly.Location);

            var references = assemblyLocations
                .Select(location => MetadataReference.CreateFromFile(location));

            // Create compilation
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                .WithOptimizationLevel(OptimizationLevel.Debug);

            return CSharpCompilation.Create("Hello.dll",
                new[] { parsedSyntaxTree },
                references: references,
                options: compilationOptions);
        }
    }
}
