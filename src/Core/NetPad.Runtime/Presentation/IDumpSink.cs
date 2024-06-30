namespace NetPad.Presentation;

public interface IDumpSink
{
    void ResultWrite<T>(T? o, DumpOptions? options = null);
    void SqlWrite<T>(T? o, DumpOptions? options = null);
}
