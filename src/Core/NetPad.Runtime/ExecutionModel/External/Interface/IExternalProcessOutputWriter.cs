using NetPad.Presentation;

namespace NetPad.ExecutionModel.External.Interface;

internal interface IExternalProcessOutputWriter
{
    Task WriteResultAsync(object? output, DumpOptions? options = null);
    Task WriteSqlAsync(object? output, DumpOptions? options = null);
}
