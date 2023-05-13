using System;
using System.Threading.Tasks;

namespace NetPad.IO;

public class AsyncActionInputReader<TInput> : IInputReader<TInput>
{
    private readonly Func<Task<TInput?>> _action;

    public AsyncActionInputReader(Func<Task<TInput?>> action)
    {
        _action = action;
    }

    public static AsyncActionInputReader<TInput> Null => new(() => Task.FromResult<TInput?>(default));

    public async Task<TInput?> ReadAsync()
    {
        return await _action();
    }
}
