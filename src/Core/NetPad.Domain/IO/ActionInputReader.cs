using System;
using System.Threading.Tasks;

namespace NetPad.IO;

public class ActionInputReader : IInputReader
{
    private readonly Func<string?> _action;

    public ActionInputReader(Func<string?> action)
    {
        _action = action;
    }

    public static ActionInputReader Null => new(() => null);

    public Task<string?> ReadAsync()
    {
        return Task.FromResult(_action());
    }
}
