using NetPad.Common;
using NetPad.IO.IPC.Stdio;
using Xunit;

namespace NetPad.Runtime.Tests.IO.IPC.Stdio;

public class StdioIpcGatewayTests
{
    private sealed record DummyMessage(string Value);

    [Fact]
    public void Listen_Then_ListenAgain_Throws()
    {
        var writer = new StringWriter();
        using var gateway = new StdioIpcGateway(writer);

        using var reader = new StringReader(Environment.NewLine);
        gateway.Listen(reader);

        Assert.Throws<InvalidOperationException>(() => gateway.Listen(new StringReader(Environment.NewLine)));
    }

    [Fact]
    public void Send_WritesEnvelope_WithIncrementingSeq_Type_And_Data()
    {
        var resolver = new AssemblyQualifiedNameTypeResolver();
        var writer = new StringWriter();
        using var gateway = new StdioIpcGateway(writer, resolver);

        var m1 = new DummyMessage("one");
        var m2 = new DummyMessage("two");

        gateway.Send(m1);
        gateway.Send(m2);

        var output = writer.ToString();
        var lines = output.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);

        var e1 = JsonSerializer.Deserialize<StdioIpcEnvelope>(lines[0]);
        var e2 = JsonSerializer.Deserialize<StdioIpcEnvelope>(lines[1]);
        Assert.NotNull(e1);
        Assert.NotNull(e2);

        Assert.Equal(1, e1!.Seq);
        Assert.Equal(2, e2!.Seq);
        Assert.Equal(resolver.GetName(typeof(DummyMessage)), e1.Type);
        Assert.Equal(resolver.GetName(typeof(DummyMessage)), e2.Type);

        var d1 = (DummyMessage?)JsonSerializer.Deserialize(e1.Data, typeof(DummyMessage));
        var d2 = (DummyMessage?)JsonSerializer.Deserialize(e2.Data, typeof(DummyMessage));
        Assert.Equal(m1, d1);
        Assert.Equal(m2, d2);
    }

    [Fact]
    public void ExecuteHandlers_InvokesRegisteredHandlers_And_Off_Unregisters()
    {
        using var gateway = new StdioIpcGateway(new StringWriter());

        var called = 0;
        Action<DummyMessage> handler = _ => called++;
        gateway.On(handler);

        gateway.ExecuteHandlers(new DummyMessage("a"));
        Assert.Equal(1, called);

        gateway.Off(handler);
        gateway.ExecuteHandlers(new DummyMessage("b"));
        Assert.Equal(1, called); // unchanged
    }

    [Fact]
    public void Subscribe_ReturnsDisposable_ThatUnregistersHandler()
    {
        using var gateway = new StdioIpcGateway(new StringWriter());
        var called = 0;
        var token = gateway.Subscribe<DummyMessage>(_ => called++);

        gateway.ExecuteHandlers(new DummyMessage("x"));
        Assert.Equal(1, called);

        token.Dispose();
        gateway.ExecuteHandlers(new DummyMessage("y"));
        Assert.Equal(1, called);
    }

    [Fact]
    public void Listen_ParsesEnvelope_And_Dispatches_To_Correct_Handler()
    {
        var resolver = new AssemblyQualifiedNameTypeResolver();

        var handled = new ManualResetEventSlim(false);
        DummyMessage? received = null;

        void Handler(DummyMessage m)
        {
            received = m;
            handled.Set();
        }

        var writer = new StringWriter();
        using var gateway = new StdioIpcGateway(writer, resolver);
        gateway.On<DummyMessage>(Handler);

        // Prepare an incoming envelope line
        var msg = new DummyMessage("hello");
        var data = JsonSerializer.Serialize(msg);
        var env = new StdioIpcEnvelope(42, resolver.GetName(typeof(DummyMessage)), data);
        var line = JsonSerializer.Serialize(env);

        using var reader = new StringReader(line + Environment.NewLine);
        gateway.Listen(reader);

        Assert.True(handled.Wait(TimeSpan.FromSeconds(2)), "Handler was not called");
        Assert.Equal(msg, received);
    }

    [Fact]
    public void Listen_WhenInputIsNotEnvelope_Invokes_OnNonMessageReceived()
    {
        using var gateway = new StdioIpcGateway(new StringWriter());

        var evt = new ManualResetEventSlim(false);
        string? got = null;
        using var reader = new StringReader($"not-json-line{Environment.NewLine}");
        gateway.Listen(reader, s =>
        {
            got = s;
            evt.Set();
        });

        Assert.True(evt.Wait(TimeSpan.FromSeconds(2)));
        Assert.Equal("not-json-line", got);
    }

    [Fact]
    public void Dispose_ClearsHandlers()
    {
        var gateway = new StdioIpcGateway(new StringWriter());
        var called = 0;
        gateway.On<DummyMessage>(_ => called++);
        gateway.Dispose();

        // After dispose handlers cleared, this should not throw or call anything
        gateway.ExecuteHandlers(new DummyMessage("z"));
        Assert.Equal(0, called);
    }
}
