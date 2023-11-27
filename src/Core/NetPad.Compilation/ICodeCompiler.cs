namespace NetPad.Compilation;

public interface ICodeCompiler
{
    CompilationResult Compile(CompilationInput input);
}
