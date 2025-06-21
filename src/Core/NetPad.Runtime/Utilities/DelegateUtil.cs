using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace NetPad.Utilities;

public static class DelegateUtil
{
    /// <summary>
    /// Creates a debounced version of the specified <see cref="Action"/>, delaying
    /// its invocation until the specified interval elapses without new calls.
    /// </summary>
    public static Action Debounce(this Action func, int milliseconds = 300)
    {
        // A subject that we push a Unit into on each call
        var subject = new Subject<Unit>();

        // When the subject goes silent for the interval, invoke the func
        subject
            .Throttle(TimeSpan.FromMilliseconds(milliseconds))
            .Subscribe(_ => func());

        // Returned delegate simply pushes into the subject
        return () => subject.OnNext(Unit.Default);
    }

    /// <summary>
    /// Creates a debounced version of the specified <see cref="Action"/>, delaying
    /// its invocation until the specified interval elapses without new calls.
    /// </summary>
    public static Action<T> Debounce<T>(this Action<T> func, int milliseconds = 300)
    {
        var subject = new Subject<T>();

        subject
            .Throttle(TimeSpan.FromMilliseconds(milliseconds))
            .Subscribe(arg => func(arg));

        return arg => subject.OnNext(arg);
    }

    /// <summary>
    /// Creates a debounced wrapper around an asynchronous <see cref="Func{Task}"/>,
    /// invoking it only after the specified interval elapses without new calls.
    /// </summary>
    public static Action DebounceAsync(this Func<Task> func, int milliseconds = 300)
    {
        var subject = new Subject<Unit>();

        subject
            .Throttle(TimeSpan.FromMilliseconds(milliseconds))
            // Use SelectMany + FromAsync so exceptions/errors flow through the Rx pipeline
            .SelectMany(_ => Observable.FromAsync(func))
            .Subscribe();

        return () => subject.OnNext(Unit.Default);
    }

    /// <summary>
    /// Creates a debounced wrapper around an asynchronous <see cref="Func{Task}"/>,
    /// invoking it only after the specified interval elapses without new calls.
    /// </summary>
    public static Action<T> DebounceAsync<T>(this Func<T, Task> func, int milliseconds = 300)
    {
        var subject = new Subject<T>();

        subject
            .Throttle(TimeSpan.FromMilliseconds(milliseconds))
            .SelectMany(arg => Observable.FromAsync(() => func(arg)))
            .Subscribe();

        return arg => subject.OnNext(arg);
    }
}
