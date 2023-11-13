using System;

namespace NetPad.Events;

public class DisposableToken : IDisposable
{
    private readonly Action _onDisposed;

    public DisposableToken(Action onDisposed)
    {
        _onDisposed = onDisposed;
    }

    public void Dispose()
    {
        _onDisposed();
    }
}
