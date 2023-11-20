namespace NetPad.Scripts;

public enum ScriptKind
{
    // TODO maybe remove Expression. Its not really needed anymore. ScriptRuntime runs expression already with Program type
    Expression = 0,
    Program = 1,
    SQL = 10
}
