namespace NetPad.Utilities;

// TODO get rid of this and substitute with a library that properly implements these methods
public static class DelegateUtil
{
    /// <summary>
    /// Creates a debounced version of the specified <see cref="Action"/>, delaying
    /// its invocation until the specified interval elapses without new calls.
    /// </summary>
    /// <param name="func">The <see cref="Action"/> to debounce.</param>
    /// <param name="milliseconds">
    /// The debounce delay in milliseconds.
    /// </param>
    /// <returns>
    /// A new <see cref="Action"/> that postpones execution of <paramref name="func"/>
    /// until after the debounce interval has elapsed.
    /// </returns>
    /// <remarks>
    /// If the returned <see cref="Action"/> is called repeatedly, the countdown restarts,
    /// and <paramref name="func"/> is only executed once no calls occur within
    /// <paramref name="milliseconds"/>.
    /// </remarks>
    public static Action Debounce(this Action func, int milliseconds = 300)
    {
        CancellationTokenSource? cancelTokenSource = null;

        return () =>
        {
            cancelTokenSource?.Cancel();
            cancelTokenSource = new CancellationTokenSource();

            Task.Delay(milliseconds, cancelTokenSource.Token)
                .ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        func();
                    }
                }, TaskScheduler.Default);
        };
    }

    /// <summary>
    /// Creates a debounced version of the specified <see cref="Action{T}"/>,
    /// delaying its invocation until the specified interval elapses without new calls.
    /// </summary>
    /// <typeparam name="T">The type of the argument passed to the action.</typeparam>
    /// <param name="func">The <see cref="Action{T}"/> to debounce.</param>
    /// <param name="milliseconds">
    /// The debounce delay in milliseconds.
    /// </param>
    /// <returns>
    /// A new <see cref="Action{T}"/> that postpones execution of <paramref name="func"/>
    /// until after the debounce interval has elapsed.
    /// </returns>
    /// <remarks>
    /// Each invocation with an argument resets the debounce timer, and only the last
    /// argument is passed to <paramref name="func"/> when the timer completes.
    /// </remarks>
    public static Action<T> Debounce<T>(this Action<T> func, int milliseconds = 300)
    {
        CancellationTokenSource? cancelTokenSource = null;

        return arg =>
        {
            cancelTokenSource?.Cancel();
            cancelTokenSource = new CancellationTokenSource();

            Task.Delay(milliseconds, cancelTokenSource.Token)
                .ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        func(arg);
                    }
                }, TaskScheduler.Default);
        };
    }

    /// <summary>
    /// Creates a debounced wrapper around an asynchronous <see cref="Func{Task}"/>,
    /// invoking it only after the specified interval elapses without new calls.
    /// </summary>
    /// <param name="func">The asynchronous <see cref="Func{Task}"/> to debounce.</param>
    /// <param name="milliseconds">
    /// The debounce delay in milliseconds.
    /// </param>
    /// <returns>
    /// An <see cref="Action"/> that, when invoked, waits for the debounce interval
    /// before calling the original asynchronous function.
    /// </returns>
    /// <remarks>
    /// If the returned <see cref="Action"/> is called multiple times, pending
    /// invocations are canceled, and only the last invocation triggers <paramref name="func"/>.
    /// </remarks>
    public static Action DebounceAsync(this Func<Task> func, int milliseconds = 300)
    {
        CancellationTokenSource? cancelTokenSource = null;

        return () =>
        {
            cancelTokenSource?.Cancel();
            cancelTokenSource = new CancellationTokenSource();

            Task.Delay(milliseconds, cancelTokenSource.Token)
                .ContinueWith(async t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        await func();
                    }
                }, TaskScheduler.Default);
        };
    }

    /// <summary>
    /// Creates a debounced wrapper around an asynchronous <see cref="Func{T, Task}"/>,
    /// invoking it only after the specified interval elapses without new calls.
    /// </summary>
    /// <typeparam name="T">The type of the argument passed to the asynchronous function.</typeparam>
    /// <param name="func">The asynchronous <see cref="Func{T, Task}"/> to debounce.</param>
    /// <param name="milliseconds">
    /// The debounce delay in milliseconds.
    /// </param>
    /// <returns>
    /// An <see cref="Action{T}"/> that, when invoked with an argument, waits for the
    /// debounce interval before calling the original asynchronous function
    /// with the last provided argument.
    /// </returns>
    /// <remarks>
    /// If multiple calls are made, previous pending calls are canceled, and only the
    /// final call within the debounce window executes <paramref name="func"/>.
    /// </remarks>
    public static Action<T> DebounceAsync<T>(this Func<T, Task> func, int milliseconds = 300)
    {
        CancellationTokenSource? cancelTokenSource = null;

        return arg =>
        {
            cancelTokenSource?.Cancel();
            cancelTokenSource = new CancellationTokenSource();

            Task.Delay(milliseconds, cancelTokenSource.Token)
                .ContinueWith(async t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        await func(arg);
                    }
                }, TaskScheduler.Default);
        };
    }
}
