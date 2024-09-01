namespace NetPad.IO;

public class AsyncActionOutputWriter<TOutput>(Func<TOutput?, string?, Task> action) : IOutputWriter<TOutput>
{
    public static AsyncActionOutputWriter<TOutput> Null => new((_, _) => Task.CompletedTask);

    public Task WriteAsync(TOutput? output, string? title = null, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        return action(output, title);
    }
}
