namespace NetPad.IO;

/// <summary>
/// An implementation of <see cref="IInputReader{TInput}"/> that executes an action when input is read (requested).
/// </summary>
/// <param name="action">The action to execute when input is read (requested).</param>
/// <typeparam name="TInput">The type of input this reader can read.</typeparam>
public class AsyncActionInputReader<TInput>(Func<Task<TInput?>> action) : IInputReader<TInput>
{
    public static AsyncActionInputReader<TInput> Null => new(() => Task.FromResult<TInput?>(default));

    public Task<TInput?> ReadAsync()
    {
        return action();
    }
}
