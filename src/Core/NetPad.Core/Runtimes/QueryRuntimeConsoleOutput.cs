using System;
using System.IO;
using System.Text;

namespace NetPad.Runtimes
{
    [Obsolete("Not using method of redirecting Console output.")]
    public class QueryRuntimeConsoleOutput : TextWriter
    {
        private readonly IQueryRuntimeOutputWriter _outputWriter;

        public override Encoding Encoding => Encoding.Default;

        public QueryRuntimeConsoleOutput(IQueryRuntimeOutputWriter outputWriter)
        {
            _outputWriter = outputWriter;
        }

        public override void Write(string? value)
        {
            _outputWriter.WriteAsync(value);
        }

        public override void WriteLine()
        {
            _outputWriter.WriteAsync("\n");
        }
    }
}
