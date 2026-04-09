using NetPad.Utilities;

namespace NetPad.Runtime.Tests.Utilities;

public class TryTests
{
    [Fact]
    public void Run_ReturnsTrue_WhenActionSucceeds()
    {
        var result = Try.Run(() => { });

        Assert.True(result);
    }

    [Fact]
    public void Run_ReturnsFalse_WhenActionThrows()
    {
        var result = Try.Run(() => throw new InvalidOperationException());

        Assert.False(result);
    }

    [Fact]
    public async Task RunAsync_ReturnsTrue_WhenActionSucceeds()
    {
        var result = await Try.RunAsync(() => Task.CompletedTask);

        Assert.True(result);
    }

    [Fact]
    public async Task RunAsync_ReturnsFalse_WhenActionThrows()
    {
        var result = await Try.RunAsync(() => throw new InvalidOperationException());

        Assert.False(result);
    }

    [Fact]
    public void RunGeneric_ReturnsValue_WhenActionSucceeds()
    {
        var result = Try.Run(() => 42);

        Assert.Equal(42, result);
    }

    [Fact]
    public void RunGeneric_ReturnsDefault_WhenActionThrows()
    {
        var result = Try.Run<int>(() => throw new InvalidOperationException());

        Assert.Equal(0, result);
    }

    [Fact]
    public void RunGeneric_ReturnsValueOnError_WhenActionThrows()
    {
        var result = Try.Run(() => throw new InvalidOperationException(), valueOnError: -1);

        Assert.Equal(-1, result);
    }

    [Fact]
    public void RunGeneric_ReturnsNullDefault_ForReferenceTypes_WhenActionThrows()
    {
        var result = Try.Run<string>(() => throw new InvalidOperationException());

        Assert.Null(result);
    }

    [Fact]
    public async Task RunAsyncGeneric_ReturnsValue_WhenActionSucceeds()
    {
        var result = await Try.RunAsync(() => Task.FromResult(42));

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task RunAsyncGeneric_ReturnsDefault_WhenActionThrows()
    {
        var result = await Try.RunAsync<int>(() => throw new InvalidOperationException());

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task RunAsyncGeneric_ReturnsValueOnError_WhenActionThrows()
    {
        var result = await Try.RunAsync<string>(
            () => throw new InvalidOperationException(),
            valueOnError: "fallback");

        Assert.Equal("fallback", result);
    }

    [Fact]
    public void RunWithErrorFunc_ReturnsValue_WhenActionSucceeds()
    {
        var result = Try.Run(() => 42, () => -1);

        Assert.Equal(42, result);
    }

    [Fact]
    public void RunWithErrorFunc_CallsErrorFunc_WhenActionThrows()
    {
        var result = Try.Run<int>(() => throw new InvalidOperationException(), () => -1);

        Assert.Equal(-1, result);
    }

    [Fact]
    public async Task RunAsyncWithErrorFunc_ReturnsValue_WhenActionSucceeds()
    {
        var result = await Try.RunAsync(() => Task.FromResult(42), () => -1);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task RunAsyncWithErrorFunc_CallsErrorFunc_WhenActionThrows()
    {
        var result = await Try.RunAsync<int>(
            () => throw new InvalidOperationException(),
            () => -1);

        Assert.Equal(-1, result);
    }
}
