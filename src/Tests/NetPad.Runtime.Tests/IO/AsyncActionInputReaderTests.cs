using NetPad.IO;
using Xunit;

namespace NetPad.Runtime.Tests.IO;

public class AsyncActionInputReaderTests
{
    [Fact]
    public async Task CallsAction()
    {
        var reader = new AsyncActionInputReader<string>(async () =>
        {
            await Task.Delay(1);
            return "My input";
        });

        var input = await reader.ReadAsync();

        Assert.Equal("My input", input);
    }

    [Fact]
    public async Task NullDoesNothing()
    {
        var reader = AsyncActionInputReader<string>.Null;

        var input = await reader.ReadAsync();

        Assert.Null(input);
    }
}
