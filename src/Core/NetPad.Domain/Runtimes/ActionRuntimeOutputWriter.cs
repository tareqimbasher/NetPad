using System;
using System.Threading.Tasks;

namespace NetPad.Runtimes
{
    public class ActionRuntimeOutputWriter : IScriptRuntimeOutputWriter
    {
        private readonly Action<object?, string?> _action;

        public ActionRuntimeOutputWriter(Action<object?, string?> action)
        {
            _action = action;
        }

        public static ActionRuntimeOutputWriter Null => new ActionRuntimeOutputWriter((_, _) => { });

        public Task WriteAsync(object? output, string? title = null)
        {
            _action(output, title);
            return Task.CompletedTask;
        }
    }

    public class AsyncActionRuntimeOutputWriter : IScriptRuntimeOutputWriter
    {
        private readonly Func<object?, string?, Task> _action;

        public AsyncActionRuntimeOutputWriter(Func<object?, string?, Task> action)
        {
            _action = action;
        }

        public static AsyncActionRuntimeOutputWriter Null =>
            new AsyncActionRuntimeOutputWriter((_, _) => Task.CompletedTask);

        public async Task WriteAsync(object? output, string? title = null)
        {
            await _action(output, title);
        }
    }
}
