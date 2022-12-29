using System.Text.Json.Serialization;

namespace NetPad.IO;

public abstract record ScriptOutput(object? Body)
{
    protected ScriptOutput(uint order, object? body) : this(body)
    {
        Order = order;
    }

    public uint Order { get; }
}

public record RawScriptOutput : ScriptOutput
{
    public RawScriptOutput() : this(0, null)
    {
    }

    public RawScriptOutput(object? body) : base(body)
    {
    }

    [JsonConstructor]
    public RawScriptOutput(uint order, string? body) : base(order, body)
    {
    }
}

public record HtmlScriptOutput : ScriptOutput
{
    public HtmlScriptOutput(string? body) : base(body)
    {
    }

    [JsonConstructor]
    public HtmlScriptOutput(uint order, string? body) : base(order, body)
    {
    }

    public new string? Body => base.Body as string;
}
