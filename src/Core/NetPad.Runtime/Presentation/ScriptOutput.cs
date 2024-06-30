using System.Text.Json.Serialization;

namespace NetPad.Presentation;

/// <summary>
/// A base class for all script output
/// </summary>
/// <param name="Body">The body of the output.</param>
public abstract record ScriptOutput(object? Body)
{
    protected ScriptOutput(uint order, object? body) : this(body)
    {
        Order = order;
    }

    /// <summary>
    /// The order this output was emitted. A value of 0 indicates no order.
    /// </summary>
    public uint Order { get; }
}

/// <summary>
/// A base class for script output with an HTML-formatted string as the body.
/// </summary>
public abstract record HtmlScriptOutput : ScriptOutput
{
    protected HtmlScriptOutput(string? body) : base(body)
    {
    }

    protected HtmlScriptOutput(uint order, string? body) : base(order, body)
    {
    }

    public new string? Body => base.Body as string;
}

/// <summary>
/// Raw script output. No modification is done to the body.
/// </summary>
public record RawScriptOutput : ScriptOutput
{
    public RawScriptOutput(object? body) : base(body)
    {
    }

    [JsonConstructor]
    public RawScriptOutput(uint order, object? body) : base(order, body)
    {
    }
}

/// <summary>
/// Raw script output represented as HTML.
/// </summary>
public record HtmlRawScriptOutput : HtmlScriptOutput
{
    public HtmlRawScriptOutput(string? body) : base(body)
    {
    }

    [JsonConstructor]
    public HtmlRawScriptOutput(uint order, string? body) : base(order, body)
    {
    }
}

/// <summary>
/// Results script output represented as HTML.
/// </summary>
public record HtmlResultsScriptOutput : HtmlScriptOutput
{
    public HtmlResultsScriptOutput(string? body) : base(body)
    {
    }

    [JsonConstructor]
    public HtmlResultsScriptOutput(uint order, string? body) : base(order, body)
    {
    }
}

/// <summary>
/// SQL script output.
/// </summary>
public record SqlScriptOutput : ScriptOutput
{
    public SqlScriptOutput(string? body) : base(body)
    {
    }

    [JsonConstructor]
    public SqlScriptOutput(uint order, string? body) : base(order, body)
    {
    }

    public new string? Body => base.Body as string;
}

/// <summary>
/// SQL script output represented as HTML.
/// </summary>
public record HtmlSqlScriptOutput : HtmlScriptOutput
{
    public HtmlSqlScriptOutput(string? body) : base(body)
    {
    }

    [JsonConstructor]
    public HtmlSqlScriptOutput(uint order, string? body) : base(order, body)
    {
    }
}

/// <summary>
/// Error script output.
/// </summary>
public record ErrorScriptOutput : ScriptOutput
{
    public ErrorScriptOutput(object? body) : base(body)
    {
    }

    [JsonConstructor]
    public ErrorScriptOutput(uint order, object? body) : base(order, body)
    {
    }
}

/// <summary>
/// Error script output represented as HTML.
/// </summary>
public record HtmlErrorScriptOutput : HtmlScriptOutput
{
    public HtmlErrorScriptOutput(string? body) : base(body)
    {
    }

    [JsonConstructor]
    public HtmlErrorScriptOutput(uint order, string? body) : base(order, body)
    {
    }
}
