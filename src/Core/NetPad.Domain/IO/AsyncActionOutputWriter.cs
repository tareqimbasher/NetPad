using System;
using System.Threading.Tasks;

namespace NetPad.IO;

public class AsyncActionOutputWriter<TOutput> : IOutputWriter<TOutput>
{
    private readonly Func<TOutput?, string?, Task> _action;

    public AsyncActionOutputWriter(Func<TOutput?, string?, Task> action)
    {
        _action = action;
    }

    public static AsyncActionOutputWriter<TOutput> Null => new((_, _) => Task.CompletedTask);

    public async Task WriteAsync(TOutput? output, string? title = null)
    {
        await _action(output, title);
    }
}
