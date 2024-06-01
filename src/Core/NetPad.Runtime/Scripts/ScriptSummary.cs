namespace NetPad.Scripts;

public class ScriptSummary(Guid id, string name, string path, ScriptKind kind)
{
    public Guid Id { get; } = id;
    public string Name { get; set; } = name;
    public string Path { get; set; } = path;
    public ScriptKind Kind { get; } = kind;
}
