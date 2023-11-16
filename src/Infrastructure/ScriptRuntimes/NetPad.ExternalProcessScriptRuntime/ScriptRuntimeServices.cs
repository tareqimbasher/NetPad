using System.Text;
using NetPad.Html;
using NetPad.IO;
using NetPad.Runtimes;

/// <summary>
/// Provides IO utils to a running script.
/// </summary>
public static class ScriptRuntimeServices
{
    // Capture default IO used by system to read/write
    private static readonly TextReader _defaultConsoleInput = Console.In;
    private static readonly TextWriter _defaultConsoleOutput = Console.Out;

    static ScriptRuntimeServices()
    {
        Output = new ExternalProcessOutputWriter(str => _defaultConsoleOutput.WriteLineAsync(str));
    }

    internal static ExternalProcessOutputWriter Output { get; private set; }

    /// <summary>
    /// Sets the specified <see cref="IOutputWriter{TOutput}"/> as the main output writer for the process.
    /// There is currently no way to overwrite main input reader, process will use STD IN (Console.In).
    /// </summary>
    /// <param name="mainOut">The main output writer for the process.</param>
    public static void SetIO(IOutputWriter<object> mainOut)
    {
        Console.SetIn(_defaultConsoleInput);
        Console.SetOut(_defaultConsoleOutput);

        Output = new ExternalProcessOutputWriter(async str => await mainOut.WriteAsync(str));
    }

    /// <summary>
    /// Uses STD IO (default Console.In/Out) as the main IO channel for the process.
    /// </summary>
    public static void UseStandardIO()
    {
        Console.SetIn(new ActionTextReader(_defaultConsoleInput));
        Console.SetOut(new ActionTextWriter((value, appendNewLine) => ResultWrite(value, appendNewLine: appendNewLine)));

        Output = new ExternalProcessOutputWriter(async str => await _defaultConsoleOutput.WriteLineAsync(str));
    }

    internal static void RawConsoleWriteLine(string text)
    {
        _defaultConsoleOutput.WriteLine(text);
    }

    public static void ResultWrite(object? o = null, string? title = null, bool appendNewLine = false)
    {
        _ = Output.WriteResultAsync(o, title, appendNewLine);
    }

    public static void SqlWrite(object? o = null, string? title = null)
    {
        _ = Output.WriteSqlAsync(o, title);
    }

    #region Extension Methods

    /// <summary>
    /// Dumps this object to the results view.
    /// </summary>
    /// <param name="o">The object to dump.</param>
    /// <param name="title">An optional title for the result.</param>
    /// <returns>The object being dumped.</returns>
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("o")]
    public static T? Dump<T>(this T? o, string? title = null)
    {
        // When using Dump() its implied that a new line is added to the end of it when rendered
        // When rendering objects or collections (ie. objects that are NOT rendered as strings)
        // they are rendered in an HTML block element that automatically pushes elements after it
        // to a new line. However when rendering strings (or objects that are rendered as strings)
        // HTML renders them in-line. So here we detect that, add manually add a new line
        bool shouldAddNewLineAfter = title == null && HtmlSerializer.IsDotNetTypeWithStringRepresentation(typeof(T));

        ResultWrite(o, title, appendNewLine: shouldAddNewLineAfter);

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
        ResultWrite(span.ToArray(), title);
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
        ResultWrite(span.ToArray(), title);
        return span;
    }

    #endregion
}

/// <summary>
/// A wrapper for all <see cref="ScriptOutput"/> emitted by external process runtime.
/// </summary>
/// <param name="Type">The type name of the specified <see cref="ScriptOutput"/>.</param>
/// <param name="Output">The wrapped <see cref="ScriptOutput"/>.</param>
public record ExternalProcessOutput(string Type, ScriptOutput? Output)
{
}

/// <summary>
/// Converts output emitted by the script (ex. using Dump() or Console.Write)
/// to <see cref="ScriptOutput"/> and writes it to the main output.
/// </summary>
public class ExternalProcessOutputWriter
{
    private readonly Func<string, System.Threading.Tasks.Task> _writeToMainOut;
    private uint _resultOutputCounter;
    private uint _sqlOutputCounter;

    public ExternalProcessOutputWriter(Func<string, System.Threading.Tasks.Task> writeToMainOut)
    {
        _writeToMainOut = writeToMainOut;
    }

    public async System.Threading.Tasks.Task WriteResultAsync(object? output, string? title = null, bool appendNewLine = false)
    {
        uint order = Interlocked.Increment(ref _resultOutputCounter);

        var html = HtmlSerializer.Serialize(output, title, appendNewLineForAllTextOutput: appendNewLine);

        var resultOutput = new HtmlResultsScriptOutput(order, html);

        await WriteAsync(new ExternalProcessOutput(nameof(HtmlResultsScriptOutput), resultOutput));
    }

    public async System.Threading.Tasks.Task WriteSqlAsync(object? output, string? title = null)
    {
        uint order = Interlocked.Increment(ref _sqlOutputCounter);

        var html = HtmlSerializer.Serialize(output, title);

        var sqlOutput = new HtmlSqlScriptOutput(order, html);

        await WriteAsync(new ExternalProcessOutput(nameof(HtmlSqlScriptOutput), sqlOutput));
    }

    private async System.Threading.Tasks.Task WriteAsync(ExternalProcessOutput processOutput)
    {
        var serializedOutput = NetPad.Common.JsonSerializer.Serialize(processOutput);

        await _writeToMainOut(serializedOutput);
    }
}


internal class ActionTextWriter : TextWriter
{
    private readonly Action<string?, bool> _write;

    public ActionTextWriter(Action<string?, bool> write)
    {
        _write = write;
    }

    public override Encoding Encoding => Encoding.Default;

    public override void Write(string? value)
    {
        _write(value, false);
    }

    public override void WriteLine(string? value)
    {
        _write(value, true);
    }

    public override void WriteLine()
    {
        _write("\n", false);
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
