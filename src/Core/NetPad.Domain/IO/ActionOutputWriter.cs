using System;
using System.Threading.Tasks;

namespace NetPad.IO;

public class ActionOutputWriter<TOutput> : IOutputWriter<TOutput>
{
    private readonly Action<TOutput?, string?> _action;

    public ActionOutputWriter(Action<TOutput?, string?> action)
    {
        _action = action;
    }

    public static ActionOutputWriter<TOutput> Null => new((_, _) => { });

    public Task WriteAsync(TOutput? output, string? title = null)
    {
        _action(output, title);
        return Task.CompletedTask;
    }
}
