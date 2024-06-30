using System.Text.Json;
using NetPad.Apps.UiInterop;

namespace NetPad.Apps.Common.Tests.UiInterop;

public class TransactionQueueTests
{
    [Fact]
    public async Task PromiseTaskIsOnlyCompletedAfterSettingResponse()
    {
        var messageId = Guid.NewGuid();
        var promise = new ResponsePromise<string>();
        IpcResponseQueue.Enqueue(messageId, promise);

        await Task.Delay(100);

        Assert.False(promise.Task.IsCompleted);

        IpcResponseQueue.ResponseReceived(messageId, JsonDocument.Parse("\"Hello world\"").RootElement);

        Assert.True(promise.Task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task ResponseIsSetCorrectly()
    {
        var messageId = Guid.NewGuid();
        var promise = new ResponsePromise<string>();
        IpcResponseQueue.Enqueue(messageId, promise);

        IpcResponseQueue.ResponseReceived(messageId, JsonDocument.Parse("\"Hello world\"").RootElement);

        Assert.Equal("Hello world", await promise.Task);
    }
}
