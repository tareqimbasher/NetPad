namespace NetPad.Events;

/// <summary>
/// A token that executes the specified action when it is disposed.
/// </summary>
public sealed class DisposableToken(Action onDispose) : IDisposable
{
    public void Dispose()
    {
        onDispose();
    }
}
