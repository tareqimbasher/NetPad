using NetPad.Presentation;
using NetPad.Presentation.Text;

namespace NetPad.ExecutionModel.External.Interface;

/// <summary>
/// Writes output emitted by the script to the console as raw text.
/// </summary>
internal class ExternalProcessOutputTextWriter(bool useConsoleColors, Func<string, Task> writeToMainOut)
    : IExternalProcessOutputWriter
{
    public async Task WriteResultAsync(object? output, DumpOptions? options = null)
    {
        options ??= DumpOptions.Default;

        var text = TextPresenter.Serialize(output, options.Title, useConsoleColors);

        await writeToMainOut(text);
    }

    public Task WriteSqlAsync(object? output, DumpOptions? options = null)
    {
        // Don't print SQL output
        return Task.CompletedTask;
    }
}
