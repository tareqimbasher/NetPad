namespace NetPad.Scripts;

public enum ScriptKind
{
    /// <summary>
    /// Not supported: these scripts open in the editor but can't be run.
    /// </summary>
    Expression = 0,
    Program = 1,
    SQL = 10
}
