using NetPad.Presentation;
using NetPad.Presentation.Html;

namespace NetPad.Runtimes;

/// <summary>
/// Converts output emitted by the script (ex. using Dump() or Console.Write)
/// to <see cref="ScriptOutput"/> and writes it to the main output.
/// </summary>
public class ExternalProcessOutputHtmlWriter : IExternalProcessOutputWriter
{
    private readonly Func<string, Task> _writeToMainOut;
    private uint _resultOutputCounter;
    private uint _sqlOutputCounter;

    public ExternalProcessOutputHtmlWriter(Func<string, Task> writeToMainOut)
    {
        _writeToMainOut = writeToMainOut;
    }

    public async Task WriteResultAsync(object? output, DumpOptions? options = null)
    {
        options ??= DumpOptions.Default;

        uint order = Interlocked.Increment(ref _resultOutputCounter);

        var html = HtmlPresenter.Serialize(output, options: options);

        var resultOutput = new HtmlResultsScriptOutput(order, html);

        await WriteAsync(new ExternalProcessOutput(nameof(HtmlResultsScriptOutput), resultOutput));
    }

    public async Task WriteSqlAsync(object? output, DumpOptions? options = null)
    {
        options ??= DumpOptions.Default;

        uint order = Interlocked.Increment(ref _sqlOutputCounter);

        var html = HtmlPresenter.Serialize(output, options: options);

        var sqlOutput = new HtmlSqlScriptOutput(order, html);

        await WriteAsync(new ExternalProcessOutput(nameof(HtmlSqlScriptOutput), sqlOutput));
    }

    private async Task WriteAsync(ExternalProcessOutput processOutput)
    {
        var serializedOutput = Common.JsonSerializer.Serialize(processOutput);

        await _writeToMainOut(serializedOutput);
    }
}
