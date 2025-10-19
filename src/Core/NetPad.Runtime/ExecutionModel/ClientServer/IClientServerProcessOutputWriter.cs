using NetPad.Presentation;

namespace NetPad.ExecutionModel.ClientServer;

public interface IClientServerProcessOutputWriter
{
    Task WriteResultAsync(object? output, DumpOptions? options = null);
    Task WriteSqlAsync(object? output, DumpOptions? options = null);
}
