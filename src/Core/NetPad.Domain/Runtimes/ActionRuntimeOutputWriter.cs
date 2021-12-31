using System;
using System.Threading.Tasks;

namespace NetPad.Runtimes
{
    public class ActionRuntimeOutputWriter : IScriptRuntimeOutputWriter
    {
        private readonly Action<object?> _action;

        public ActionRuntimeOutputWriter(Action<object?> action)
        {
            _action = action;
        }

        public static ActionRuntimeOutputWriter Null => new ActionRuntimeOutputWriter(o => { });

        public Task WriteAsync(object? output)
        {
            _action(output);
            return Task.CompletedTask;
        }
    }

    public class AsyncActionRuntimeOutputWriter : IScriptRuntimeOutputWriter
    {
        private readonly Func<object?, Task> _action;

        public AsyncActionRuntimeOutputWriter(Func<object?, Task> action)
        {
            _action = action;
        }

        public static AsyncActionRuntimeOutputWriter Null =>
            new AsyncActionRuntimeOutputWriter(o => Task.CompletedTask);

        public async Task WriteAsync(object? output)
        {
            await _action(output);
        }
    }
}
