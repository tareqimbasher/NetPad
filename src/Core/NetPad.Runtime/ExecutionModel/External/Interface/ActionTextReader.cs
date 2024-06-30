using System.IO;

namespace NetPad.ExecutionModel.External.Interface;

internal class ActionTextReader(Func<string?> readLine) : TextReader
{
    public override string? ReadLine()
    {
        return readLine();
    }
}
