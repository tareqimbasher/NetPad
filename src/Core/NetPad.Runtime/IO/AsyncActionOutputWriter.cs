namespace NetPad.IO;

/// <summary>
/// An implementation of <see cref="IOutputWriter{TOutput}"/> that executes an async delegate when output is written.
/// </summary>
/// <param name="action">The action to execute when output is written.</param>
/// <typeparam name="TOutput">The type of output this writer can write.</typeparam>
public class AsyncActionOutputWriter<TOutput>(Func<TOutput?, string?, Task> action) : IOutputWriter<TOutput>
{
    public static AsyncActionOutputWriter<TOutput> Null => new((_, _) => Task.CompletedTask);

    public Task WriteAsync(TOutput? output, string? title = null, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        return action(output, title);
    }
}
