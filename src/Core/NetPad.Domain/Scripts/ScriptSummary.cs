using System;

namespace NetPad.Scripts;

public class ScriptSummary
{
    public ScriptSummary(Guid id, string name, string path)
    {
        Id = id;
        Name = name;
        Path = path;
    }

    public Guid Id { get; }
    public string Name { get; set; }
    public string Path { get; set; }
}
