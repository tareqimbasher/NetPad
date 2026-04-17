using NetPad.Utilities;

namespace NetPad.Runtime.Tests.Utilities;

public class DelegateUtilTests
{
    private static readonly TimeSpan _fireTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan _settleDelay = TimeSpan.FromMilliseconds(200);

    [Fact]
    public async Task Debounce_InvokesActionAfterInterval()
    {
        int callCount = 0;
        var fired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var debounced = ((Action)(() =>
        {
            Interlocked.Increment(ref callCount);
            fired.TrySetResult();
        })).Debounce(50);

        debounced();
        await fired.Task.WaitAsync(_fireTimeout);

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task Debounce_CollapsesRapidCalls()
    {
        int callCount = 0;
        var fired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var debounced = ((Action)(() =>
        {
            Interlocked.Increment(ref callCount);
            fired.TrySetResult();
        })).Debounce(100);

        debounced();
        debounced();
        debounced();
        await fired.Task.WaitAsync(_fireTimeout);
        await Task.Delay(_settleDelay);

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task DebounceGeneric_PassesLastArgument()
    {
        string? lastValue = null;
        var fired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var debounced = ((Action<string>)(v =>
        {
            lastValue = v;
            fired.TrySetResult();
        })).Debounce(50);

        debounced("first");
        debounced("second");
        debounced("third");
        await fired.Task.WaitAsync(_fireTimeout);

        Assert.Equal("third", lastValue);
    }

    [Fact]
    public async Task DebounceAsync_InvokesActionAfterInterval()
    {
        int callCount = 0;
        var fired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var debounced = ((Func<Task>)(() =>
        {
            Interlocked.Increment(ref callCount);
            fired.TrySetResult();
            return Task.CompletedTask;
        })).DebounceAsync(50);

        debounced();
        await fired.Task.WaitAsync(_fireTimeout);

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task DebounceAsync_CollapsesRapidCalls()
    {
        int callCount = 0;
        var fired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var debounced = ((Func<Task>)(() =>
        {
            Interlocked.Increment(ref callCount);
            fired.TrySetResult();
            return Task.CompletedTask;
        })).DebounceAsync(100);

        debounced();
        debounced();
        debounced();
        await fired.Task.WaitAsync(_fireTimeout);
        await Task.Delay(_settleDelay);

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task DebounceAsyncGeneric_PassesLastArgument()
    {
        string? lastValue = null;
        var fired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var debounced = ((Func<string, Task>)(v =>
        {
            lastValue = v;
            fired.TrySetResult();
            return Task.CompletedTask;
        })).DebounceAsync(50);

        debounced("first");
        debounced("second");
        debounced("third");
        await fired.Task.WaitAsync(_fireTimeout);

        Assert.Equal("third", lastValue);
    }
}
