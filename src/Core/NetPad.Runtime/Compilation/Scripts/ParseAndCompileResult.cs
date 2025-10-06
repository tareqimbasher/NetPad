namespace NetPad.Compilation.Scripts;


/// <summary>
/// Represents the combined results of parsing and compiling a script.
/// </summary>
/// <param name="ParsingResult">
/// The result of analyzing the script's source code and building a compilable version of the code.
/// </param>
/// <param name="CompilationResult">
/// The result of compiling the parsed script, including emitted assembly information and diagnostics.
/// </param>
public record ParseAndCompileResult(CodeParsingResult ParsingResult, CompilationResult CompilationResult);
