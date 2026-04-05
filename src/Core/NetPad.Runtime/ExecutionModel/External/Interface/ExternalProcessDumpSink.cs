using System.IO;
using NetPad.IO;
using NetPad.Presentation;
using NetPad.Presentation.Html;

namespace NetPad.ExecutionModel.External.Interface;

/// <summary>
/// Provides IO utils to a running script.
/// </summary>
public class ExternalProcessDumpSink : IDumpSink
{
    private static readonly TextReader _defaultConsoleInput;
    private static readonly TextWriter _defaultConsoleOutput;
    private static IExternalProcessOutputWriter? _output;
    private static bool _isHtmlOutput;
    private static readonly Lazy<ExternalProcessDumpSink> _instance = new(() => new ExternalProcessDumpSink());
    private static readonly object _drainLock = new();
    private static readonly List<Task> _pendingWrites = [];

    static ExternalProcessDumpSink()
    {
        // Capture default IO
        _defaultConsoleInput = Console.In;
        _defaultConsoleOutput = Console.Out;

        // Drain pending async writes before process exit to prevent output loss.
        // Writes are fire-and-forget during execution (to keep user code fast),
        // but must complete before the process terminates.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Drain();
    }

    private ExternalProcessDumpSink()
    {
    }

    public static ExternalProcessDumpSink Instance => _instance.Value;

    public void UseHtmlOutput(bool dumpRawHtml)
    {
        Console.SetIn(new ActionTextReader(() =>
        {
            RawConsoleWriteLine("[INPUT_REQUEST]");
            return _defaultConsoleInput.ReadLine();
        }));
        Console.SetOut(new ActionTextWriter((value, appendNewLine) =>
            ResultWrite(value, new DumpOptions(AppendNewLineToAllTextOutput: appendNewLine))));

        _isHtmlOutput = true;
        _output = new ExternalProcessOutputHtmlWriter(
            async str => await _defaultConsoleOutput.WriteLineAsync(str),
            dumpRawHtml);
    }

    public void UseJsonOutput(bool dumpRawJson, bool includeSql)
    {
        Console.SetIn(new ActionTextReader(() =>
        {
            RawConsoleWriteLine("[INPUT_REQUEST]");
            return _defaultConsoleInput.ReadLine();
        }));
        Console.SetOut(new ActionTextWriter((value, appendNewLine) =>
            ResultWrite(value, new DumpOptions(AppendNewLineToAllTextOutput: appendNewLine))));

        _isHtmlOutput = false;
        _output = new ExternalProcessOutputJsonWriter(
            async str => await _defaultConsoleOutput.WriteLineAsync(str), dumpRawJson, includeSql);
    }

    public void UseConsoleOutput(bool plainText, bool minimal)
    {
        Console.SetIn(_defaultConsoleInput);
        Console.SetOut(_defaultConsoleOutput);

        _isHtmlOutput = false;
        _output = new ExternalProcessOutputConsoleWriter(plainText, minimal);
    }

    public void RawConsoleWriteLine(string text)
    {
        _defaultConsoleOutput.WriteLine(text);
    }

    public void ResultWrite<T>(T? o, DumpOptions? options = null)
    {
        options ??= new DumpOptions();

        if (_isHtmlOutput && options.AppendNewLineToAllTextOutput == null)
        {
            // When using Dump() its implied that a new line is added to the end of it when rendered
            // When rendering objects or collections (ie. objects that are NOT rendered as strings)
            // they are rendered in an HTML block element that automatically pushes elements after it
            // to a new line. However when rendering strings (or objects that are rendered as strings)
            // HTML renders them in-line. So here we detect that, and manually add a new line
            bool shouldAddNewLineAfter =
                options.Title == null || HtmlPresenter.IsDotNetTypeWithStringRepresentation(typeof(T));

            if (shouldAddNewLineAfter)
            {
                options = options with { AppendNewLineToAllTextOutput = true };
            }
        }

        TrackWrite(_output?.WriteResultAsync(o, options));
    }

    public void SqlWrite<T>(T? o, DumpOptions? options = null)
    {
        TrackWrite(_output?.WriteSqlAsync(o, options));
    }

    private static void TrackWrite(Task? task)
    {
        if (task == null || task.IsCompleted) return;
        lock (_drainLock)
        {
            if (_pendingWrites.Count >= 1000)
            {
                _pendingWrites.RemoveAll(t => t.IsCompleted);
            }

            _pendingWrites.Add(task);
        }
    }

    /// <summary>
    /// Blocks until all pending async output writes have completed.
    /// Called automatically on process exit to prevent output loss.
    /// </summary>
    public static void Drain()
    {
        lock (_drainLock)
        {
            if (_pendingWrites.Count > 0)
            {
                Task.WhenAll(_pendingWrites).GetAwaiter().GetResult();
                _pendingWrites.Clear();
            }
        }

        _defaultConsoleOutput.Flush();
    }
}
