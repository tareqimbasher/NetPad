using NetPad.Presentation;
using NetPad.Presentation.Console;

namespace NetPad.ExecutionModel.External.Interface;

/// <summary>
/// Writes output emitted by the script to the console as console-formatted text.
/// </summary>
internal class ExternalProcessOutputConsoleWriter(bool plainText) : IExternalProcessOutputWriter
{
    public Task WriteResultAsync(object? output, DumpOptions? options = null)
    {
        options ??= new DumpOptions();

        ConsolePresenter.Serialize(output, options.Title, plainText);

        return Task.CompletedTask;
    }

    public Task WriteSqlAsync(object? output, DumpOptions? options = null)
    {
        // Don't print SQL output
        return Task.CompletedTask;
    }
}
