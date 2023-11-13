using System;
using System.Threading;
using System.Threading.Tasks;

namespace NetPad.IO;

/// <summary>
/// An <see cref="IOutputWriter{TOutput}"/> that internally executes a specific action.
/// </summary>
public class ActionOutputWriter<TOutput> : IOutputWriter<TOutput>
{
    private readonly Action<TOutput?, string?> _action;

    public ActionOutputWriter(Action<TOutput?, string?> action)
    {
        _action = action;
    }

    public static ActionOutputWriter<TOutput> Null => new((_, _) => { });

    public Task WriteAsync(TOutput? output, string? title = null, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        _action(output, title);
        return Task.CompletedTask;
    }
}
