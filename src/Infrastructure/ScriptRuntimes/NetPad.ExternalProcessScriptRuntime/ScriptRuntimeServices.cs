using System.Text;
using NetPad.Html;
using NetPad.IO;

public static class ScriptRuntimeServices
{
    private static TextWriter _defaultConsoleOutput;
    private static IScriptOutputAdapter<object, object>? _output;

    public static void Init()
    {
        // Capture default TextWriter used by system to write to the console
        _defaultConsoleOutput = Console.Out;

        // Redirect standard console output
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
    /// <param name="o">The object being dumped.</param>
    /// <param name="title">An optional title for the result.</param>
    /// <returns>The object being dumped.</returns>
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("o")]
    public static T? Dump<T>(this T? o, string? title = null)
    {
        ScriptRuntimeServices.ResultWrite(o, title);
        return o;
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
