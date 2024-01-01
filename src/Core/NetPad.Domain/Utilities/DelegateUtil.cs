using System;
using System.Threading;
using System.Threading.Tasks;

namespace NetPad.Utilities;

public static class DelegateUtil
{
    public static Action Debounce(this Action func, int milliseconds = 300, bool immediate = false)
    {
        CancellationTokenSource? cancelTokenSource = null;
        Task? task = null;

        return () =>
        {
            cancelTokenSource?.Cancel();
            cancelTokenSource = new CancellationTokenSource();

            if (immediate && (task == null || task.IsCompleted))
            {
                func();
            }

            task = Task.Delay(milliseconds, cancelTokenSource.Token)
                .ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        func();
                    }
                }, TaskScheduler.Default);
        };
    }

    public static Action<T> Debounce<T>(this Action<T> func, int milliseconds = 300, bool immediate = false)
    {
        CancellationTokenSource? cancelTokenSource = null;
        Task? task = null;

        return arg =>
        {
            cancelTokenSource?.Cancel();
            cancelTokenSource = new CancellationTokenSource();

            if (immediate && (task == null || task.IsCompleted))
            {
                func(arg);
            }

            task = Task.Delay(milliseconds, cancelTokenSource.Token)
                .ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        func(arg);
                    }
                }, TaskScheduler.Default);
        };
    }

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
