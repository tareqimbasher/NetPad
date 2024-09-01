namespace NetPad.IO;

public class AsyncActionInputReader<TInput>(Func<Task<TInput?>> action) : IInputReader<TInput>
{
    public static AsyncActionInputReader<TInput> Null => new(() => Task.FromResult<TInput?>(default));

    public Task<TInput?> ReadAsync()
    {
        return action();
    }
}
