using System;
using System.Threading.Tasks;

namespace NetPad.Runtimes
{
    public class ActionRuntimeInputReader : IScriptRuntimeInputReader
    {
        private readonly Func<string?> _action;

        public ActionRuntimeInputReader(Func<string?> action)
        {
            _action = action;
        }

        public static ActionRuntimeInputReader Null => new ActionRuntimeInputReader(() => null);

        public Task<string?> ReadAsync()
        {
            return Task.FromResult(_action());
        }
    }

    public class AsyncActionRuntimeInputReader : IScriptRuntimeInputReader
    {
        private readonly Func<Task<string?>> _action;

        public AsyncActionRuntimeInputReader(Func<Task<string?>> action)
        {
            _action = action;
        }

        public static AsyncActionRuntimeInputReader Null =>
            new AsyncActionRuntimeInputReader(() => Task.FromResult<string?>(null));

        public async Task<string?> ReadAsync()
        {
            return await _action();
        }
    }
}
