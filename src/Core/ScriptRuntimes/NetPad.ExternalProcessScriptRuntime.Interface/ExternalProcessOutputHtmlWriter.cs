using NetPad.Presentation.Html;

namespace NetPad.Runtimes;

/// <summary>
/// Converts output emitted by the script (ex. using Dump() or Console.Write)
/// to <see cref="ScriptOutput"/> and writes it to the main output.
/// </summary>
public class ExternalProcessOutputHtmlWriter : IExternalProcessOutputWriter
{
    private readonly Func<string, System.Threading.Tasks.Task> _writeToMainOut;
    private uint _resultOutputCounter;
    private uint _sqlOutputCounter;

    public ExternalProcessOutputHtmlWriter(Func<string, System.Threading.Tasks.Task> writeToMainOut)
    {
        _writeToMainOut = writeToMainOut;
    }

    public async System.Threading.Tasks.Task WriteResultAsync(object? output, string? title = null, bool appendNewLine = false)
    {
        uint order = Interlocked.Increment(ref _resultOutputCounter);

        var html = HtmlPresenter.Serialize(output, title, appendNewLineForAllTextOutput: appendNewLine);

        var resultOutput = new HtmlResultsScriptOutput(order, html);

        await WriteAsync(new ExternalProcessOutput(nameof(HtmlResultsScriptOutput), resultOutput));
    }

    public async System.Threading.Tasks.Task WriteSqlAsync(object? output, string? title = null)
    {
        uint order = Interlocked.Increment(ref _sqlOutputCounter);

        var html = HtmlPresenter.Serialize(output, title);

        var sqlOutput = new HtmlSqlScriptOutput(order, html);

        await WriteAsync(new ExternalProcessOutput(nameof(HtmlSqlScriptOutput), sqlOutput));
    }

    private async System.Threading.Tasks.Task WriteAsync(ExternalProcessOutput processOutput)
    {
        var serializedOutput = NetPad.Common.JsonSerializer.Serialize(processOutput);

        await _writeToMainOut(serializedOutput);
    }
}
