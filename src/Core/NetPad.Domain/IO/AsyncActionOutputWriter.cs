using System;
using System.Threading.Tasks;

namespace NetPad.IO;

public class AsyncActionOutputWriter : IOutputWriter
{
    private readonly Func<object?, string?, Task> _action;

    public AsyncActionOutputWriter(Func<object?, string?, Task> action)
    {
        _action = action;
    }

    public static AsyncActionOutputWriter Null =>
        new AsyncActionOutputWriter((_, _) => Task.CompletedTask);

    public async Task WriteAsync(object? output, string? title = null)
    {
        await _action(output, title);
    }
}
