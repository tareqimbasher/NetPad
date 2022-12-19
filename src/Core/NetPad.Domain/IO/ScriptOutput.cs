namespace NetPad.IO;

public record ScriptOutput(object? Body);

public record RawScriptOutput(object? Body) : ScriptOutput(Body);

public record HtmlScriptOutput : ScriptOutput
{
    public HtmlScriptOutput(string? body) : base(body)
    {
    }

    public new string? Body => base.Body as string;
}
