using System.Text.RegularExpressions;
using NetPad.Presentation;
using NetPad.Presentation.Html;

namespace NetPad.ExecutionModel.External.Interface;

/// <summary>
/// Converts output emitted by the script (ex. using Dump() or Console.Write)
/// to <see cref="ScriptOutput"/> and writes it to the main output.
/// </summary>
public class ExternalProcessOutputHtmlWriter(Func<string, Task> writeToMainOut) : IExternalProcessOutputWriter
{
    private static readonly Lazy<Regex> _ansiColorsRegex = new(() => new Regex(@"\x1B\[[^@-~]*[@-~]"));
    private uint _resultOutputCounter;
    private uint _sqlOutputCounter;

    public async Task WriteResultAsync(object? output, DumpOptions? options = null)
    {
        options ??= DumpOptions.Default;

        uint order = Interlocked.Increment(ref _resultOutputCounter);

        // Added because ASP.NET Core output includes ANSI color formatting on Windows OS
        if (output is string str && str.StartsWith("\u001B[", StringComparison.Ordinal))
        {
            output = _ansiColorsRegex.Value.Replace(str, string.Empty);
        }

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

        await writeToMainOut(serializedOutput);
    }
}
