using System.Collections.Concurrent;
using NetPad.ExecutionModel;
using NetPad.IO;
using NetPad.Presentation;

namespace NetPad.Runtime.Tests.ExecutionModel;

public class RawOutputHandlerTests
{
    /// <summary>
    /// A test output writer that captures all writes for assertion.
    /// </summary>
    private class CapturingOutputWriter : IOutputWriter<object>
    {
        public ConcurrentBag<object> Writes { get; } = [];

        public Task WriteAsync(object? output, string? title = null, CancellationToken cancellationToken = default)
        {
            if (output != null)
            {
                Writes.Add(output);
            }

            return Task.CompletedTask;
        }
    }

    private static (RawOutputHandler handler, CapturingOutputWriter writer) Create()
    {
        var writer = new CapturingOutputWriter();
        var handler = new RawOutputHandler(writer);
        return (handler, writer);
    }

    /// <summary>
    /// The debounce interval is 300ms. We wait long enough for it to fire.
    /// </summary>
    private static Task WaitForDebounce() => Task.Delay(500);

    [Fact]
    public async Task RawOutputReceived_WritesRawScriptOutput()
    {
        var (handler, writer) = Create();

        handler.RawOutputReceived("hello");
        await WaitForDebounce();

        var output = Assert.Single(writer.Writes);
        var raw = Assert.IsType<ScriptOutput>(output);
        Assert.Equal(ScriptOutputKind.Result, raw.Kind);
        Assert.Equal("hello\n", raw.Body);
    }

    [Fact]
    public async Task RawErrorReceived_WritesErrorScriptOutput()
    {
        var (handler, writer) = Create();

        handler.RawErrorReceived("fail");
        await WaitForDebounce();

        var output = Assert.Single(writer.Writes);
        var error = Assert.IsType<ScriptOutput>(output);
        Assert.Equal(ScriptOutputKind.Error, error.Kind);
        Assert.Equal("fail\n", error.Body);
    }

    [Fact]
    public async Task RapidOutputs_AreBatchedIntoOneWrite()
    {
        var (handler, writer) = Create();

        handler.RawOutputReceived("line1");
        handler.RawOutputReceived("line2");
        handler.RawOutputReceived("line3");
        await WaitForDebounce();

        var output = Assert.Single(writer.Writes);
        var raw = Assert.IsType<ScriptOutput>(output);
        Assert.Equal(ScriptOutputKind.Result, raw.Kind);
        Assert.Equal("line1\nline2\nline3\n", raw.Body);
    }

    [Fact]
    public async Task RapidErrors_AreBatchedIntoOneWrite()
    {
        var (handler, writer) = Create();

        handler.RawErrorReceived("err1");
        handler.RawErrorReceived("err2");
        handler.RawErrorReceived("err3");
        await WaitForDebounce();

        var output = Assert.Single(writer.Writes);
        var error = Assert.IsType<ScriptOutput>(output);
        Assert.Equal(ScriptOutputKind.Error, error.Kind);
        Assert.Equal("err1\nerr2\nerr3\n", error.Body);
    }

    [Fact]
    public async Task BatchedOutputs_PreserveInsertionOrder()
    {
        var (handler, writer) = Create();

        handler.RawOutputReceived("c");
        handler.RawOutputReceived("a");
        handler.RawOutputReceived("b");
        await WaitForDebounce();

        var output = Assert.Single(writer.Writes);
        var raw = Assert.IsType<ScriptOutput>(output);
        Assert.Equal(ScriptOutputKind.Result, raw.Kind);
        Assert.Equal("c\na\nb\n", raw.Body);
    }

    [Fact]
    public async Task OutputAndError_AreIndependentStreams()
    {
        var (handler, writer) = Create();

        handler.RawOutputReceived("stdout");
        handler.RawErrorReceived("stderr");
        await WaitForDebounce();

        Assert.Equal(2, writer.Writes.Count);
        Assert.Single(writer.Writes.OfType<ScriptOutput>(), o => o.Kind == ScriptOutputKind.Result);
        Assert.Single(writer.Writes.OfType<ScriptOutput>(), o => o.Kind == ScriptOutputKind.Error);
    }

    [Fact]
    public async Task Reset_ZeroesOrderCounters()
    {
        var (handler, writer) = Create();

        handler.RawOutputReceived("first");
        await WaitForDebounce();

        handler.Reset();

        handler.RawOutputReceived("second");
        await WaitForDebounce();

        var outputs = writer.Writes.OfType<ScriptOutput>().Where(o => o.Kind == ScriptOutputKind.Result).ToList();
        Assert.Equal(2, outputs.Count);

        // Both should have Order == 0 since Reset was called between them
        Assert.All(outputs, o => Assert.Equal(0u, o.Order));
    }

    [Fact]
    public async Task SpacedOutCalls_ProduceSeparateWrites()
    {
        var (handler, writer) = Create();

        handler.RawOutputReceived("batch1");
        await WaitForDebounce();

        handler.RawOutputReceived("batch2");
        await WaitForDebounce();

        var outputs = writer.Writes.OfType<ScriptOutput>()
            .Where(o => o.Kind == ScriptOutputKind.Result)
            .OrderBy(o => o.Order)
            .ToList();
        Assert.Equal(2, outputs.Count);
        Assert.Equal("batch1\n", outputs[0].Body);
        Assert.Equal("batch2\n", outputs[1].Body);
    }
}
