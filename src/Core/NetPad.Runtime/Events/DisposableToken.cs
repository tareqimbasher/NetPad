namespace NetPad.Events;

public class DisposableToken(Action onDispose) : IDisposable
{
    public void Dispose()
    {
        onDispose();
    }
}
