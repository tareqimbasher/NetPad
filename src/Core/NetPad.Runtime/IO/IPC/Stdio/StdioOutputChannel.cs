using System.IO;
using NetPad.Common;

namespace NetPad.IO.IPC.Stdio;

internal class StdioOutputChannel(TextWriter writer)
{
    public void Write<T>(T message)
    {
        writer.WriteLine(JsonSerializer.Serialize(message));
    }
}
