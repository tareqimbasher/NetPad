using NetPad.Utilities;

namespace NetPad.Runtime.Tests.Utilities;

public class DelegateUtilTests
{
    [Fact]
    public async Task Debounce_InvokesActionAfterInterval()
    {
        int callCount = 0;
        var debounced = ((Action)(() => callCount++)).Debounce(50);

        debounced();
        await Task.Delay(200);

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task Debounce_CollapsesRapidCalls()
    {
        int callCount = 0;
        var debounced = ((Action)(() => callCount++)).Debounce(100);

        debounced();
        debounced();
        debounced();
        await Task.Delay(300);

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task DebounceGeneric_PassesLastArgument()
    {
        string? lastValue = null;
        var debounced = ((Action<string>)(v => lastValue = v)).Debounce(50);

        debounced("first");
        debounced("second");
        debounced("third");
        await Task.Delay(200);

        Assert.Equal("third", lastValue);
    }

    [Fact]
    public async Task DebounceAsync_InvokesActionAfterInterval()
    {
        int callCount = 0;
        var debounced = ((Func<Task>)(() =>
        {
            callCount++;
            return Task.CompletedTask;
        })).DebounceAsync(50);

        debounced();
        await Task.Delay(200);

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task DebounceAsync_CollapsesRapidCalls()
    {
        int callCount = 0;
        var debounced = ((Func<Task>)(() =>
        {
            callCount++;
            return Task.CompletedTask;
        })).DebounceAsync(100);

        debounced();
        debounced();
        debounced();
        await Task.Delay(300);

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task DebounceAsyncGeneric_PassesLastArgument()
    {
        string? lastValue = null;
        var debounced = ((Func<string, Task>)(v =>
        {
            lastValue = v;
            return Task.CompletedTask;
        })).DebounceAsync(50);

        debounced("first");
        debounced("second");
        debounced("third");
        await Task.Delay(200);

        Assert.Equal("third", lastValue);
    }
}
