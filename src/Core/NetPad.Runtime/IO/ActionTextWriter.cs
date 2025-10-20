using System.IO;
using System.Text;

namespace NetPad.IO;

internal class ActionTextWriter(Action<object?, bool> write) : TextWriter
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

    public override void Write(char value)
    {
        write(value, false);
    }

    public override void WriteLine(char value)
    {
        write(value, true);
    }

    public override void WriteLine()
    {
        write("\n", false);
    }
}
