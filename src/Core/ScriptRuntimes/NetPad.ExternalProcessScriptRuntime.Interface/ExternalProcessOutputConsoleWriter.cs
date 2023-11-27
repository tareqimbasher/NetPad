using NetPad.Presentation.Console;

namespace NetPad.Runtimes;

/// <summary>
/// Writes output emitted by the script to the console as console-formatted text.
/// </summary>
internal class ExternalProcessOutputConsoleWriter : IExternalProcessOutputWriter
{
    private readonly bool _useConsoleColors;

    public ExternalProcessOutputConsoleWriter(bool useConsoleColors)
    {
        _useConsoleColors = useConsoleColors;
    }

    public System.Threading.Tasks.Task WriteResultAsync(object? output, string? title = null, bool appendNewLine = false)
    {
        ConsolePresenter.Serialize(output, title, _useConsoleColors);

        return System.Threading.Tasks.Task.CompletedTask;
    }

    public System.Threading.Tasks.Task WriteSqlAsync(object? output, string? title = null)
    {
        ConsolePresenter.Serialize(output, title, _useConsoleColors);

        return System.Threading.Tasks.Task.CompletedTask;
    }
}
