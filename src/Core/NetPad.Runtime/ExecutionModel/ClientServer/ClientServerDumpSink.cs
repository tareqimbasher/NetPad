using NetPad.ExecutionModel.External.Interface;
using NetPad.Presentation;
using NetPad.Presentation.Html;

namespace NetPad.ExecutionModel.ClientServer;

/// <summary>
/// Wires up IO for a running script.
/// </summary>
public class ClientServerDumpSink : IDumpSink
{
    private static IExternalProcessOutputWriter? _output;
    private static bool _isHtmlOutput;
    private static readonly Lazy<ClientServerDumpSink> _instance = new(() => new ClientServerDumpSink());

    private ClientServerDumpSink()
    {
    }

    public static ClientServerDumpSink Instance => _instance.Value;

    public void RedirectStdIO(Func<string, Task> onWrite, Func<string?> onRequestInput)
    {
        _isHtmlOutput = true;
        _output = new ClientServerOutputHtmlWriter(async str => await onWrite(str));

        Console.SetOut(new ActionTextWriter((value, appendNewLine) =>
            ResultWrite(value, new DumpOptions(AppendNewLineToAllTextOutput: appendNewLine))));
        Console.SetIn(new ActionTextReader(onRequestInput));
    }

    public void ResultWrite<T>(T? o, DumpOptions? options = null)
    {
        options ??= DumpOptions.Default;

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

        _ = _output?.WriteResultAsync(o, options);
    }

    public void SqlWrite<T>(T? o, DumpOptions? options = null)
    {
        _ = _output?.WriteSqlAsync(o, options);
    }
}
