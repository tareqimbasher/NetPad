using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace NetPad.Compilation
{
    public class CompilationInput
    {
        public CompilationInput(string code, IEnumerable<byte[]>? assemblyImageReferences = null, IEnumerable<string>? assemblyFileReferences = null)
        {
            // Default to OutputKind.ConsoleApplication instead OutputKind.DynamicallyLinkedLibrary so the generated assembly
            // is able to be executed as an executable (ie. dotnet ./assembly.dll). Using OutputKind.DynamicallyLinkedLibrary
            // generates an assembly that does not have an entry point, resulting in failure to execute standalone assembly
            // as an external process.
            // Another reason to use OutputKind.ConsoleApplication is we are using top-level statements, which we cannot
            // compile the assembly with if set to OutputKind.DynamicallyLinkedLibrary.
            OutputKind = OutputKind.ConsoleApplication;
            Code = code;
            AssemblyImageReferences = assemblyImageReferences?.ToArray() ?? Array.Empty<byte[]>();
            AssemblyFileReferences = assemblyFileReferences?.ToArray() ?? Array.Empty<string>();
        }

        public OutputKind OutputKind { get; private set; }
        public string Code { get; }
        public string? OutputAssemblyNameTag { get; private set; }
        public byte[][] AssemblyImageReferences { get; }
        public string[] AssemblyFileReferences { get; }

        public CompilationInput WithOutputKind(OutputKind outputKind)
        {
            OutputKind = outputKind;
            return this;
        }

        public CompilationInput WithOutputAssemblyNameTag(string? outputAssemblyNameTag)
        {
            OutputAssemblyNameTag = outputAssemblyNameTag;
            return this;
        }
    }
}
