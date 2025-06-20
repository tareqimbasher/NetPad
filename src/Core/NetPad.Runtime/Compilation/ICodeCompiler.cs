namespace NetPad.Compilation;

public interface ICodeCompiler
{
    /// <summary>
    /// Compiles .NET code.
    /// </summary>
    CompilationResult Compile(CompilationInput input);
}
