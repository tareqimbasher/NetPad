using System;
using System.Threading.Tasks;

namespace NetPad.Runtimes
{
    public class TestQueryRuntimeOutputReader : IQueryRuntimeOutputReader
    {
        private readonly Action<object?> _action;

        public TestQueryRuntimeOutputReader(Action<object?> action)
        {
            _action = action;
        }
        
        public Task ReadAsync(object? output)
        {
            _action(output);
            return Task.CompletedTask;
        }
    }
}