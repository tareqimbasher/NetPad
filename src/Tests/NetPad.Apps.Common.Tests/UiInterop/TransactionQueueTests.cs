using System.Text.Json;
using NetPad.Apps.CQs;
using NetPad.Apps.UiInterop;

namespace NetPad.Apps.Common.Tests.UiInterop;

public class TransactionQueueTests
{
    class TestCommand : Command<string>;

    [Fact]
    public async Task PromiseTaskIsOnlyCompletedAfterSettingResponse()
    {
        var message = new TestCommand();

        var promise = IpcResponseQueue.Enqueue(message);
        await Task.Delay(100);

        Assert.False(promise.Task.IsCompleted);

        using var doc1 = JsonDocument.Parse("\"Hello world\"");
        IpcResponseQueue.ResponseReceived(message.RequestId, doc1.RootElement);

        Assert.True(promise.Task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task ResponseIsSetCorrectly()
    {
        var message = new TestCommand();

        var promise = IpcResponseQueue.Enqueue(message);
        await Task.Delay(100);

        Assert.False(promise.Task.IsCompleted);

        using var doc2 = JsonDocument.Parse("\"Hello world\"");
        IpcResponseQueue.ResponseReceived(message.RequestId, doc2.RootElement);

        Assert.Equal("Hello world", await promise.Task);
    }
}
