using System.IO;
using System.Text;

namespace NetPad.ExecutionModel.External.Interface;

internal class ActionTextWriter(Action<string?, bool> write) : TextWriter
{
    public override Encoding Encoding => Encoding.Default;

    public override void Write(string? value)
    {
        write(value, false);
    }

    public override void WriteLine(string? value)
    {
        write(value, true);
    }

    public override void WriteLine()
    {
        write("\n", false);
    }
}
