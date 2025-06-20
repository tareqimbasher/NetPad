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

        IpcResponseQueue.ResponseReceived(message.Id, JsonDocument.Parse("\"Hello world\"").RootElement);

        Assert.True(promise.Task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task ResponseIsSetCorrectly()
    {
        var message = new TestCommand();

        var promise = IpcResponseQueue.Enqueue(message);
        await Task.Delay(100);

        Assert.False(promise.Task.IsCompleted);

        IpcResponseQueue.ResponseReceived(message.Id, JsonDocument.Parse("\"Hello world\"").RootElement);

        Assert.Equal("Hello world", await promise.Task);
    }
}
