using System;
using System.Threading.Tasks;

namespace NetPad.Runtimes
{
    public class TestScriptRuntimeOutputWriter : IScriptRuntimeOutputWriter
    {
        private readonly Action<object?> _action;

        public TestScriptRuntimeOutputWriter(Action<object?> action)
        {
            _action = action;
        }
        
        public Task WriteAsync(object? output)
        {
            _action(output);
            return Task.CompletedTask;
        }
    }
}