using NetPad.Presentation;
using NetPad.Presentation.Text;

namespace NetPad.Runtimes;

/// <summary>
/// Writes output emitted by the script to the console as raw text.
/// </summary>
internal class ExternalProcessOutputTextWriter : IExternalProcessOutputWriter
{
    private readonly bool _useConsoleColors;
    private readonly Func<string, Task> _writeToMainOut;

    public ExternalProcessOutputTextWriter(bool useConsoleColors, Func<string, Task> writeToMainOut)
    {
        _useConsoleColors = useConsoleColors;
        _writeToMainOut = writeToMainOut;
    }

    public async Task WriteResultAsync(object? output, DumpOptions? options = null)
    {
        options ??= DumpOptions.Default;

        var text = TextPresenter.Serialize(output, options.Title, _useConsoleColors);

        await _writeToMainOut(text);
    }

    public async Task WriteSqlAsync(object? output, DumpOptions? options = null)
    {
        options ??= DumpOptions.Default;

        var text = TextPresenter.Serialize(output, options.Title, _useConsoleColors);

        await _writeToMainOut(text);
    }
}
