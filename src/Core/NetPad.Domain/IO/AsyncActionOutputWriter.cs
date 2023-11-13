using System;
using System.Threading;
using System.Threading.Tasks;

namespace NetPad.IO;

/// <summary>
/// An <see cref="IOutputWriter{TOutput}"/> that internally executes a specific async action.
/// </summary>
public class AsyncActionOutputWriter<TOutput> : IOutputWriter<TOutput>
{
    private readonly Func<TOutput?, string?, Task> _action;

    public AsyncActionOutputWriter(Func<TOutput?, string?, Task> action)
    {
        _action = action;
    }

    public static AsyncActionOutputWriter<TOutput> Null => new((_, _) => Task.CompletedTask);

    public async Task WriteAsync(TOutput? output, string? title = null, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        await _action(output, title);
    }
}
