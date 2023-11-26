using NetPad.Presentation.Text;

namespace NetPad.Runtimes;

/// <summary>
/// Writes output emitted by the script to the console as raw text.
/// </summary>
internal class ExternalProcessOutputTextWriter : IExternalProcessOutputWriter
{
    private readonly bool _useConsoleColors;
    private readonly Func<string, Task> _writeToMainOut;

    public ExternalProcessOutputTextWriter(bool useConsoleColors, Func<string, System.Threading.Tasks.Task> writeToMainOut)
    {
        _useConsoleColors = useConsoleColors;
        _writeToMainOut = writeToMainOut;
    }

    public async System.Threading.Tasks.Task WriteResultAsync(object? output, string? title = null, bool appendNewLine = false)
    {
        var text = TextPresenter.Serialize(output, title, _useConsoleColors);

        await _writeToMainOut(text);
    }

    public async System.Threading.Tasks.Task WriteSqlAsync(object? output, string? title = null)
    {
        var text = TextPresenter.Serialize(output, title, _useConsoleColors);

        await _writeToMainOut(text);
    }
}
