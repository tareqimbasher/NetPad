using System;
using System.IO;
using System.Text;

namespace NetPad.Runtimes
{
    [Obsolete("Not using method of redirecting Console output anymore.")]
    public class ScriptRuntimeConsoleOutput : TextWriter
    {
        private readonly IScriptRuntimeOutputWriter _outputWriter;

        public override Encoding Encoding => Encoding.Default;

        public ScriptRuntimeConsoleOutput(IScriptRuntimeOutputWriter outputWriter)
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
