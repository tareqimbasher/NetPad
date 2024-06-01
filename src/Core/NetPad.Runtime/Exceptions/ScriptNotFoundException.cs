namespace NetPad.Exceptions;

public class ScriptNotFoundException : Exception
{
    public ScriptNotFoundException(Guid scriptId) : base($"No script found with id: {scriptId}")
    {
    }

    public ScriptNotFoundException(string path) : base($"No script found with path: {path}")
    {
    }
}
