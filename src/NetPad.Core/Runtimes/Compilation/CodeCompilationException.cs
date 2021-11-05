using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace NetPad.Runtimes.Compilation
{
    public class CodeCompilationException : Exception
    {
        public CodeCompilationException(string message, ImmutableArray<Diagnostic> resultDiagnostics) : base(message)
        {
            Errors = resultDiagnostics;
        }
        
        public ImmutableArray<Diagnostic> Errors { get; }
        public string ErrorsAsString() => string.Join("\n", Errors.Select(e => e.ToString()));
    }
}