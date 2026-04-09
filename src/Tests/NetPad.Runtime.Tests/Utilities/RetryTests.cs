using NetPad.Utilities;

namespace NetPad.Runtime.Tests.Utilities;

public class RetryTests
{
    [Fact]
    public void Execute_SucceedsOnFirstAttempt()
    {
        int callCount = 0;

        Retry.Execute(3, TimeSpan.Zero, () => callCount++);

        Assert.Equal(1, callCount);
    }

    [Fact]
    public void Execute_RetriesAndSucceeds()
    {
        int callCount = 0;

        Retry.Execute(3, TimeSpan.Zero, () =>
        {
            callCount++;
            if (callCount < 2)
                throw new InvalidOperationException("fail");
        });

        Assert.Equal(2, callCount);
    }

    [Fact]
    public void Execute_ThrowsAggregateException_WhenAllAttemptsFail()
    {
        int callCount = 0;

        var ex = Assert.Throws<AggregateException>(() =>
            Retry.Execute(3, TimeSpan.Zero, () =>
            {
                callCount++;
                throw new InvalidOperationException($"fail {callCount}");
            }));

        Assert.Equal(3, callCount);
        Assert.Equal(3, ex.InnerExceptions.Count);
    }

    [Fact]
    public void Execute_WithSingleAttempt_ThrowsOnFailure()
    {
        var ex = Assert.Throws<AggregateException>(() =>
            Retry.Execute(1, TimeSpan.Zero, () => throw new InvalidOperationException("fail")));

        Assert.Single(ex.InnerExceptions);
    }

    [Fact]
    public async Task ExecuteAsync_SucceedsOnFirstAttempt()
    {
        int callCount = 0;

        await Retry.ExecuteAsync(3, TimeSpan.Zero, async () =>
        {
            callCount++;
            await Task.CompletedTask;
        });

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task ExecuteAsync_RetriesAndSucceeds()
    {
        int callCount = 0;

        await Retry.ExecuteAsync(3, TimeSpan.Zero, async () =>
        {
            callCount++;
            if (callCount < 3)
                throw new InvalidOperationException("fail");
            await Task.CompletedTask;
        });

        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsAggregateException_WhenAllAttemptsFail()
    {
        int callCount = 0;

        var ex = await Assert.ThrowsAsync<AggregateException>(() =>
            Retry.ExecuteAsync(2, TimeSpan.Zero, () =>
            {
                callCount++;
                throw new InvalidOperationException($"fail {callCount}");
            }));

        Assert.Equal(2, callCount);
        Assert.Equal(2, ex.InnerExceptions.Count);
    }

    [Fact]
    public async Task ExecuteAsyncWithResult_ReturnsValue_OnSuccess()
    {
        var result = await Retry.ExecuteAsync(3, TimeSpan.Zero, () => Task.FromResult(42));

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteAsyncWithResult_RetriesAndReturnsValue()
    {
        int callCount = 0;

        var result = await Retry.ExecuteAsync(3, TimeSpan.Zero, () =>
        {
            callCount++;
            if (callCount < 2)
                throw new InvalidOperationException("fail");
            return Task.FromResult("success");
        });

        Assert.Equal("success", result);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task ExecuteAsyncWithResult_ThrowsAggregateException_WhenAllAttemptsFail()
    {
        var ex = await Assert.ThrowsAsync<AggregateException>(() =>
            Retry.ExecuteAsync<int>(3, TimeSpan.Zero, () =>
                throw new InvalidOperationException("fail")));

        Assert.Equal(3, ex.InnerExceptions.Count);
    }
}
