using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace NetPad.Exceptions
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
