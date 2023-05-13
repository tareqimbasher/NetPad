using System;
using System.Threading.Tasks;

namespace NetPad.IO;

public class ActionInputReader<TInput> : IInputReader<TInput>
{
    private readonly Func<TInput?> _action;

    public ActionInputReader(Func<TInput?> action)
    {
        _action = action;
    }

    public static ActionInputReader<TInput> Null => new(() => default);

    public Task<TInput?> ReadAsync()
    {
        return Task.FromResult(_action());
    }
}
