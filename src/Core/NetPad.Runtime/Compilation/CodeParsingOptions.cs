using NetPad.DotNet.CodeAnalysis;

namespace NetPad.Compilation;

public class CodeParsingOptions
{
    /// <summary>
    /// Additional code to include in the program.
    /// </summary>
    public SourceCodeCollection AdditionalCode { get; init; } = [];

    /// <summary>
    /// Whether code parser should add ASP.NET namespaces.
    /// </summary>
    public bool IncludeAspNetUsings { get; set; }
}
