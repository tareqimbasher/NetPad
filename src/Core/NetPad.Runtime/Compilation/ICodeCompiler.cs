namespace NetPad.Compilation;

/// <summary>
/// Compiles user script code.
/// </summary>
public interface ICodeCompiler
{
    CompilationResult Compile(CompilationInput input);
}
