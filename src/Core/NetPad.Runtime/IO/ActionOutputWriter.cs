namespace NetPad.IO;

/// <summary>
/// An implementation of <see cref="IOutputWriter{TOutput}"/> that executes an action when output is written.
/// </summary>
/// <param name="action">The action to execute when output is written.</param>
/// <typeparam name="TOutput">The type of output this writer can write.</typeparam>
public class ActionOutputWriter<TOutput>(Action<TOutput?, string?> action) : IOutputWriter<TOutput>
{
    public static ActionOutputWriter<TOutput> Null => new((_, _) => { });

    public Task WriteAsync(TOutput? output, string? title = null, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        action(output, title);
        return Task.CompletedTask;
    }
}
