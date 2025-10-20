using System.IO;

namespace NetPad.IO;

internal class ActionTextReader(Func<string?> readLine) : TextReader
{
    public override string? ReadLine()
    {
        return readLine();
    }
}
