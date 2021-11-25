namespace NetPad.Compilation
{
    public interface ICodeCompiler
    {
        byte[] Compile(CompilationInput input);
    }
}
