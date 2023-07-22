using System.Text;
using NetPad.Html;
using NetPad.IO;

public static class ScriptRuntimeServices
{
    private static TextReader _defaultConsoleInput;
    private static TextWriter _defaultConsoleOutput;
    private static IScriptOutputAdapter<object, object>? _output;

    public static void Init()
    {
        // Capture default IO used by system to read/write
        _defaultConsoleInput = Console.In;
        _defaultConsoleOutput = Console.Out;

        // Redirect standard console IO
        Console.SetIn(new ActionTextReader(_defaultConsoleInput));
        Console.SetOut(new ActionTextWriter(new ActionOutputWriter<object>((o, _) => ResultWrite(o))));
    }

    internal static IScriptOutputAdapter<object, object> Output
    {
        get
        {
            _output ??= new ScriptOutputAdapter<object, object>(
                new ExternalProcessOutputWriter(ExternalProcessOutputChannel.Results, _defaultConsoleOutput),
                new ExternalProcessOutputWriter(ExternalProcessOutputChannel.Sql, _defaultConsoleOutput)
            );

            return _output;
        }
    }

    public static void SetIO(IScriptOutputAdapter<object, object>? output)
    {
        _output = output;
    }

    public static void RawConsoleWriteLine(string text)
    {
        _defaultConsoleOutput.WriteLine(text);
    }

    public static void ResultWrite(object? o = null, string? title = null)
    {
        Output.ResultsChannel.WriteAsync(o, title);
    }

    public static void SqlWrite(object? o = null, string? title = null)
    {
        Output.SqlChannel?.WriteAsync(o, title);
    }
}

public static class Extensions
{
    /// <summary>
    /// Dumps this object to the results view.
    /// </summary>
    /// <param name="o">The object to dump.</param>
    /// <param name="title">An optional title for the result.</param>
    /// <returns>The object being dumped.</returns>
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("o")]
    public static T? Dump<T>(this T? o, string? title = null)
    {
        ScriptRuntimeServices.ResultWrite(o, title);

        // When using Dump() its implied that a new line is added to the end of it when rendered
        // When rendering objects or collections (ie. objects that are NOT rendered as strings)
        // they are rendered in an HTML block element that automatically pushes elements after it
        // to a new line. However when rendering strings (or objects that are rendered as strings)
        // HTML renders them in-line. So here we detect that, add manually add a new line
        if (title == null && HtmlSerializer.IsDotNetTypeWithStringRepresentation(typeof(T)))
            ScriptRuntimeServices.ResultWrite("\n");

        return o;
    }

    /// <summary>
    /// Dumps this <see cref="Span{T}"/> to the results view.
    /// </summary>
    /// <param name="span">The <see cref="Span{T}"/> to dump.</param>
    /// <param name="title">An optional title for the result.</param>
    /// <returns>The <see cref="Span{T}"/> being dumped.</returns>
    public static Span<T> Dump<T>(this Span<T> span, string? title = null)
    {
        ScriptRuntimeServices.ResultWrite(span.ToArray(), title);
        return span;
    }

    /// <summary>
    /// Dumps this <see cref="ReadOnlySpan{T}"/> to the results view.
    /// </summary>
    /// <param name="span">The <see cref="ReadOnlySpan{T}"/> to dump.</param>
    /// <param name="title">An optional title for the result.</param>
    /// <returns>The <see cref="ReadOnlySpan{T}"/> being dumped.</returns>
    public static ReadOnlySpan<T> Dump<T>(this ReadOnlySpan<T> span, string? title = null)
    {
        ScriptRuntimeServices.ResultWrite(span.ToArray(), title);
        return span;
    }
}

public class ExternalProcessOutputWriter : IOutputWriter<object>
{
    private readonly ExternalProcessOutputChannel _channel;
    private readonly TextWriter _defaultConsoleOutput;
    private uint _outputCounter;

    public ExternalProcessOutputWriter(ExternalProcessOutputChannel channel, TextWriter defaultConsoleOutput)
    {
        _channel = channel;
        _defaultConsoleOutput = defaultConsoleOutput;
    }

    public async System.Threading.Tasks.Task WriteAsync(object? output, string? title = null)
    {
        var html = HtmlSerializer.Serialize(output, title);

        var processOutput = new ExternalProcessOutput<HtmlScriptOutput>(
            _channel,
            new HtmlScriptOutput(Interlocked.Increment(ref _outputCounter), html));

        var serializedOutput = NetPad.Common.JsonSerializer.Serialize(processOutput);

        await _defaultConsoleOutput.WriteLineAsync(serializedOutput);
    }
}

public enum ExternalProcessOutputChannel
{
    Unknown = 0,
    Results = 1,
    Sql = 2
}

public record ExternalProcessOutput<TOutput>(ExternalProcessOutputChannel Channel, TOutput? Output);

internal class ActionTextWriter : TextWriter
{
    private readonly IOutputWriter<object> _outputWriter;

    public ActionTextWriter(IOutputWriter<object> outputWriter)
    {
        _outputWriter = outputWriter;
    }

    public override Encoding Encoding => Encoding.Default;

    public override void Write(string? value)
    {
        _outputWriter.WriteAsync(value);
    }

    public override void WriteLine()
    {
        _outputWriter.WriteAsync("\n");
    }
}

internal class ActionTextReader : TextReader
{
    private readonly TextReader _defaultConsoleInput;

    public ActionTextReader(TextReader defaultConsoleInput)
    {
        _defaultConsoleInput = defaultConsoleInput;
    }

    public override string? ReadLine()
    {
        ScriptRuntimeServices.RawConsoleWriteLine("[INPUT_REQUEST]");
        var input = _defaultConsoleInput.ReadLine();
        return input;
    }
}
