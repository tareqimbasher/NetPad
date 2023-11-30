using NetPad.Presentation;
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

    public Task WriteResultAsync(object? output, DumpOptions? options = null)
    {
        options ??= DumpOptions.Default;

        ConsolePresenter.Serialize(output, options.Title, _useConsoleColors);

        return Task.CompletedTask;
    }

    public Task WriteSqlAsync(object? output, DumpOptions? options = null)
    {
        options ??= DumpOptions.Default;

        ConsolePresenter.Serialize(output, options.Title, _useConsoleColors);

        return Task.CompletedTask;
    }
}
