using NetPad.IO;
using Xunit;

namespace NetPad.Runtime.Tests.IO;

public class AsyncActionOutputWriterTests
{
    [Fact]
    public async Task CallsAction()
    {
        var result = "";
        var writer = new AsyncActionOutputWriter<string>(async (output, _) =>
        {
            await Task.Delay(1);
            result += output;
        });

        await writer.WriteAsync("Hello World!");

        Assert.Equal("Hello World!", result);
    }

    [Fact]
    public async Task CallsActionWithTitle()
    {
        var result = "";
        var writer = new AsyncActionOutputWriter<string>(async (output, title) =>
        {
            await Task.Delay(1);
            result += $"{title}: {output}";
        });

        await writer.WriteAsync("Hello World!", "My Title");

        Assert.Equal("My Title: Hello World!", result);
    }

    [Fact]
    public async Task CallsActionWithCancellation()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var result = "";
        var writer = new AsyncActionOutputWriter<string>(async (output, _) =>
        {
            await Task.Delay(1);
            result += output;
        });

        await writer.WriteAsync("Hello World!", "My Title", cancellationTokenSource.Token);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task NullDoesNothing()
    {
        var result = "";
        var writer = AsyncActionOutputWriter<string>.Null;

        await writer.WriteAsync("Hello World!");

        Assert.Equal(string.Empty, result);
    }
}
