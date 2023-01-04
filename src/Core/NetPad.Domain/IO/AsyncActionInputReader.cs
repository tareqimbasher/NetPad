using System;
using System.Threading.Tasks;

namespace NetPad.IO;

public class AsyncActionInputReader : IInputReader
{
    private readonly Func<Task<string?>> _action;

    public AsyncActionInputReader(Func<Task<string?>> action)
    {
        _action = action;
    }

    public static AsyncActionInputReader Null => new(() => Task.FromResult<string?>(null));

    public async Task<string?> ReadAsync()
    {
        return await _action();
    }
}
