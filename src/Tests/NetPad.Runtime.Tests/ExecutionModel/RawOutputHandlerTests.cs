using System.Collections.Concurrent;
using NetPad.ExecutionModel;
using NetPad.IO;
using NetPad.Presentation;

namespace NetPad.Runtime.Tests.ExecutionModel;

public class RawOutputHandlerTests
{
    private static readonly TimeSpan _flushTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan _settleDelay = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// A test output writer that captures all writes and releases a semaphore per write.
    /// Tests await the semaphore to know a flush happened rather than relying on Task.Delay.
    /// </summary>
    private class CapturingOutputWriter : IOutputWriter<object>
    {
        public ConcurrentBag<object> Writes { get; } = [];
        public SemaphoreSlim WriteSignal { get; } = new(0);

        public Task WriteAsync(object? output, string? title = null, CancellationToken cancellationToken = default)
        {
            if (output != null)
            {
                Writes.Add(output);
                WriteSignal.Release();
            }

            return Task.CompletedTask;
        }

        public async Task WaitForWritesAsync(int expected)
        {
            for (int i = 0; i < expected; i++)
            {
                var ok = await WriteSignal.WaitAsync(_flushTimeout);
                if (!ok) throw new TimeoutException($"Timed out waiting for write {i + 1} of {expected}.");
            }

            await Task.Delay(_settleDelay);
            Assert.Equal(0, WriteSignal.CurrentCount);
        }
    }

    private static (RawOutputHandler handler, CapturingOutputWriter writer) Create()
    {
        var writer = new CapturingOutputWriter();
        var handler = new RawOutputHandler(writer);
        return (handler, writer);
    }

    [Fact]
    public async Task RawOutputReceived_WritesRawScriptOutput()
    {
        var (handler, writer) = Create();

        handler.RawOutputReceived("hello");
        await writer.WaitForWritesAsync(1);

        var output = Assert.Single(writer.Writes);
        var raw = Assert.IsType<RawScriptOutput>(output);
        Assert.Equal("hello\n", raw.Body);
    }

    [Fact]
    public async Task RawErrorReceived_WritesErrorScriptOutput()
    {
        var (handler, writer) = Create();

        handler.RawErrorReceived("fail");
        await writer.WaitForWritesAsync(1);

        var output = Assert.Single(writer.Writes);
        var error = Assert.IsType<ErrorScriptOutput>(output);
        Assert.Equal("fail\n", error.Body);
    }

    [Fact]
    public async Task RapidOutputs_AreBatchedIntoOneWrite()
    {
        var (handler, writer) = Create();

        handler.RawOutputReceived("line1");
        handler.RawOutputReceived("line2");
        handler.RawOutputReceived("line3");
        await writer.WaitForWritesAsync(1);

        var output = Assert.Single(writer.Writes);
        var raw = Assert.IsType<RawScriptOutput>(output);
        Assert.Equal("line1\nline2\nline3\n", raw.Body);
    }

    [Fact]
    public async Task RapidErrors_AreBatchedIntoOneWrite()
    {
        var (handler, writer) = Create();

        handler.RawErrorReceived("err1");
        handler.RawErrorReceived("err2");
        handler.RawErrorReceived("err3");
        await writer.WaitForWritesAsync(1);

        var output = Assert.Single(writer.Writes);
        var error = Assert.IsType<ErrorScriptOutput>(output);
        Assert.Equal("err1\nerr2\nerr3\n", error.Body);
    }

    [Fact]
    public async Task BatchedOutputs_PreserveInsertionOrder()
    {
        var (handler, writer) = Create();

        handler.RawOutputReceived("c");
        handler.RawOutputReceived("a");
        handler.RawOutputReceived("b");
        await writer.WaitForWritesAsync(1);

        var output = Assert.Single(writer.Writes);
        var raw = Assert.IsType<RawScriptOutput>(output);
        Assert.Equal("c\na\nb\n", raw.Body);
    }

    [Fact]
    public async Task OutputAndError_AreIndependentStreams()
    {
        var (handler, writer) = Create();

        handler.RawOutputReceived("stdout");
        handler.RawErrorReceived("stderr");
        await writer.WaitForWritesAsync(2);

        Assert.Equal(2, writer.Writes.Count);
        Assert.Single(writer.Writes.OfType<RawScriptOutput>());
        Assert.Single(writer.Writes.OfType<ErrorScriptOutput>());
    }

    [Fact]
    public async Task Reset_ZeroesOrderCounters()
    {
        var (handler, writer) = Create();

        handler.RawOutputReceived("first");
        await writer.WaitForWritesAsync(1);

        handler.Reset();

        handler.RawOutputReceived("second");
        await writer.WaitForWritesAsync(1);

        var outputs = writer.Writes.OfType<RawScriptOutput>().ToList();
        Assert.Equal(2, outputs.Count);

        // Both should have Order == 0 since Reset was called between them
        Assert.All(outputs, o => Assert.Equal(0u, o.Order));
    }

    [Fact]
    public async Task SpacedOutCalls_ProduceSeparateWrites()
    {
        var (handler, writer) = Create();

        handler.RawOutputReceived("batch1");
        await writer.WaitForWritesAsync(1);

        handler.RawOutputReceived("batch2");
        await writer.WaitForWritesAsync(1);

        var outputs = writer.Writes.OfType<RawScriptOutput>().OrderBy(o => o.Order).ToList();
        Assert.Equal(2, outputs.Count);
        Assert.Equal("batch1\n", outputs[0].Body);
        Assert.Equal("batch2\n", outputs[1].Body);
    }
}
