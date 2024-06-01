namespace NetPad.Utilities;

public static class AsyncUtil
{
    /// <summary>
    /// Executes an async Task which has a void return value synchronously
    /// </summary>
    /// <param name="func">Task to execute</param>
    public static void RunSync(Func<Task> func)
    {
        var oldContext = SynchronizationContext.Current;
        var synch = new ExclusiveSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(synch);
        // ReSharper disable once AsyncVoidLambda
        synch.Post(async _ =>
        {
            try
            {
                await func();
            }
            catch (Exception e)
            {
                synch.InnerException = e;
                throw;
            }
            finally
            {
                synch.EndMessageLoop();
            }
        }, null);
        synch.BeginMessageLoop();

        SynchronizationContext.SetSynchronizationContext(oldContext);
    }

    /// <summary>
    /// Executes an async Task which has a T return type synchronously
    /// </summary>
    /// <typeparam name="T">Return Type</typeparam>
    /// <param name="func">Task to execute</param>
    /// <returns></returns>
    public static T? RunSync<T>(Func<Task<T>> func)
    {
        var oldContext = SynchronizationContext.Current;
        var synch = new ExclusiveSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(synch);
        T? ret = default(T?);
        // ReSharper disable once AsyncVoidLambda
        synch.Post(async _ =>
        {
            try
            {
                ret = await func();
            }
            catch (Exception e)
            {
                synch.InnerException = e;
                throw;
            }
            finally
            {
                synch.EndMessageLoop();
            }
        }, null);
        synch.BeginMessageLoop();
        SynchronizationContext.SetSynchronizationContext(oldContext);
        return ret;
    }

    private class ExclusiveSynchronizationContext : SynchronizationContext
    {
        private bool _done;
        private readonly AutoResetEvent _workItemsWaiting = new(false);
        private readonly Queue<Tuple<SendOrPostCallback, object>> items = new();

        public Exception? InnerException { get; set; }

        public override void Send(SendOrPostCallback d, object? state)
        {
            throw new NotSupportedException("We cannot send to our same thread");
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            lock (items)
            {
                items.Enqueue(Tuple.Create(d, state)!);
            }

            _workItemsWaiting.Set();
        }

        public void EndMessageLoop()
        {
            Post(_ => _done = true, null);
        }

        public void BeginMessageLoop()
        {
            while (!_done)
            {
                Tuple<SendOrPostCallback, object>? task = null;
                lock (items)
                {
                    if (items.Count > 0)
                    {
                        task = items.Dequeue();
                    }
                }

                if (task != null)
                {
                    task.Item1(task.Item2);
                    if (InnerException != null) // the method threw an exception
                    {
                        throw new AggregateException("AsyncHelpers.Run method threw an exception.", InnerException);
                    }
                }
                else
                {
                    _workItemsWaiting.WaitOne();
                }
            }
        }

        public override SynchronizationContext CreateCopy()
        {
            return this;
        }
    }
}
