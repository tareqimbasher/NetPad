using NetPad.DotNet;

namespace NetPad.Compilation;

public class CodeParsingOptions
{
    /// <summary>
    /// Additional code to include in the program.
    /// </summary>
    public SourceCodeCollection AdditionalCode { get; init; } = new();

    /// <summary>
    /// Whether code parser should add add ASP.NET namespaces.
    /// </summary>
    public bool IncludeAspNetUsings { get; init; }
}
