using System.Text;

namespace NetPad.Runtimes;

internal class ActionTextWriter : TextWriter
{
    private readonly Action<string?, bool> _write;

    public ActionTextWriter(Action<string?, bool> write)
    {
        _write = write;
    }

    public override Encoding Encoding => Encoding.Default;

    public override void Write(string? value)
    {
        _write(value, false);
    }

    public override void WriteLine(string? value)
    {
        _write(value, true);
    }

    public override void WriteLine()
    {
        _write("\n", false);
    }
}
