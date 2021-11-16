using System.IO;
using System.Text;

namespace NetPad.Runtimes
{
    public class QueryRuntimeOutputReaderTextWriter : TextWriter
    {
        private readonly IQueryRuntimeOutputReader _outputReader;

        public override Encoding Encoding => Encoding.Default;

        public QueryRuntimeOutputReaderTextWriter(IQueryRuntimeOutputReader outputReader)
        {
            _outputReader = outputReader;
        }

        public override void Write(string? value)
        {
            _outputReader.ReadAsync(value);
        }

        public override void WriteLine()
        {
            _outputReader.ReadAsync("\n");
        }
    }
}
