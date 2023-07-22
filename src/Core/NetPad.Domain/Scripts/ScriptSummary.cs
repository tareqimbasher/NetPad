using System;

namespace NetPad.Scripts;

public class ScriptSummary
{
    public ScriptSummary(Guid id, string name, string path, ScriptKind kind)
    {
        Id = id;
        Name = name;
        Path = path;
        Kind = kind;
    }

    public Guid Id { get; }
    public string Name { get; set; }
    public string Path { get; set; }
    public ScriptKind Kind { get; }
}
