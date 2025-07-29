namespace NetPad.IO;
/// <summary>
/// An implementation of <see cref="IInputReader{TInput}"/> that executes an action when input is requested.
/// </summary>
/// <param name="action">The action to execute when input is requested.</param>
/// <typeparam name="TInput">The type of input this reader can request.</typeparam>
public class ActionInputReader<TInput>(Func<TInput?> action) : IInputReader<TInput>
{
    public static ActionInputReader<TInput> Null => new(() => default);

    public Task<TInput?> ReadAsync()
    {
        return Task.FromResult(action());
    }
}
