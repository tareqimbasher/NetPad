using System;
using System.Threading.Tasks;

namespace NetPad.IO
{
    public class ActionOutputWriter : IOutputWriter
    {
        private readonly Action<object?, string?> _action;

        public ActionOutputWriter(Action<object?, string?> action)
        {
            _action = action;
        }

        public static ActionOutputWriter Null => new ActionOutputWriter((_, _) => { });

        public Task WriteAsync(object? output, string? title = null)
        {
            _action(output, title);
            return Task.CompletedTask;
        }
    }
}
