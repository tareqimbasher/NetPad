using System;
using System.IO;
using System.Linq;
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
            string assemblyName = "NetPadScript";

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

            var references = assemblyLocations
                .Where(al => !string.IsNullOrWhiteSpace(al))
                .Select(location => MetadataReference.CreateFromFile(location));

            // Use OutputKind.ConsoleApplication vs OutputKind.DynamicallyLinkedLibrary so the generated assembly
            // is able to be executed as an executable (ie. dotnet ./assembly.dll). Using OutputKind.DynamicallyLinkedLibrary
            // generates an assembly that does not have an entry point, resulting in failure to execute standalone assembly
            // in external processes outside of NetPad
            var outputKind = OutputKind.ConsoleApplication;

            var compilationOptions = new CSharpCompilationOptions(outputKind)
                .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                .WithOptimizationLevel(OptimizationLevel.Debug)
                .WithOverflowChecks(true);

            return CSharpCompilation.Create(assemblyName,
                new[] { parsedSyntaxTree },
                references: references,
                options: compilationOptions);
        }

        public CSharpParseOptions GetParseOptions()
        {
            // TODO investigate using SourceKind.Script
            return CSharpParseOptions.Default
                .WithLanguageVersion(LanguageVersion.CSharp9)
                .WithKind(SourceCodeKind.Regular);
        }
    }
}
