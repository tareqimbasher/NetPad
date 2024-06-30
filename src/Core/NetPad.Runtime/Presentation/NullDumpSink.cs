namespace NetPad.Presentation;

/// <summary>
/// A dump sink that does nothing.
/// </summary>
internal class NullDumpSink : IDumpSink
{
    public void ResultWrite<T>(T? o, DumpOptions? options = null)
    {
    }

    public void SqlWrite<T>(T? o, DumpOptions? options = null)
    {
    }
}
