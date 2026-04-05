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

    public override void Write(char[] buffer, int index, int count)
    {
        write(new string(buffer, index, count), false);
    }

    public override void Write(ReadOnlySpan<char> buffer)
    {
        write(new string(buffer), false);
    }

    public override void WriteLine(ReadOnlySpan<char> buffer)
    {
        write(new string(buffer), true);
    }
}
