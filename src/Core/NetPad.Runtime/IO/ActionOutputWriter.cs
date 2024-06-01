namespace NetPad.IO;

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
