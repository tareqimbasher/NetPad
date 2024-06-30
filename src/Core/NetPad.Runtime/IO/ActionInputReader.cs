namespace NetPad.IO;

public class ActionInputReader<TInput>(Func<TInput?> action) : IInputReader<TInput>
{
    public static ActionInputReader<TInput> Null => new(() => default);

    public Task<TInput?> ReadAsync()
    {
        return Task.FromResult(action());
    }
}
