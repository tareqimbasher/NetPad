using NetPad.DotNet.CodeAnalysis;

namespace NetPad.Compilation;

/// <summary>
/// Options that the <see cref="ICodeParser"/> uses when user script code is parsed.
/// </summary>
public class CodeParsingOptions
{
    /// <summary>
    /// Additional code to include in the program.
    /// </summary>
    public SourceCodeCollection AdditionalCode { get; init; } = [];

    /// <summary>
    /// Additional usings to include in parsed program.
    /// </summary>
    public string[] AdditionalUsings { get; init; } = [];

    /// <summary>
    /// Whether code parser should add ASP.NET namespaces.
    /// </summary>
    public bool IncludeAspNetUsings { get; set; }
}
