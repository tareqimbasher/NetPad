using NetPad.Presentation;

namespace NetPad.Runtimes;

internal interface IExternalProcessOutputWriter
{
    Task WriteResultAsync(object? output, DumpOptions? options = null);
    Task WriteSqlAsync(object? output, DumpOptions? options = null);
}
