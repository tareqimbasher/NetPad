namespace NetPad.Events;

public sealed class DisposableToken(Action onDispose) : IDisposable
{
    public void Dispose()
    {
        onDispose();
    }
}
