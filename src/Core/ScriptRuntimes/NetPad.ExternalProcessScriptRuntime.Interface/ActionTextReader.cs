namespace NetPad.Runtimes;

internal class ActionTextReader : TextReader
{
    private readonly Func<string?> _readLine;

    public ActionTextReader(Func<string?> readLine)
    {
        _readLine = readLine;
    }

    public override string? ReadLine()
    {
        return _readLine();
    }
}
