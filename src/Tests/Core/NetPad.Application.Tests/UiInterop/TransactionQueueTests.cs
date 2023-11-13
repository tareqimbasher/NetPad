using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using NetPad.UiInterop;

namespace NetPad.Application.Tests.UiInterop;

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
    public void ResponseIsSetCorrectly()
    {
        var messageId = Guid.NewGuid();
        var promise = new ResponsePromise<string>();
        IpcResponseQueue.Enqueue(messageId, promise);

        IpcResponseQueue.ResponseReceived(messageId, JsonDocument.Parse("\"Hello world\"").RootElement);

        Assert.Equal("Hello world", promise.Task.Result);
    }
}
