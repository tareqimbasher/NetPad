using NetPad.IO;

namespace NetPad.Runtime.Tests.IO;

public class ActionInputReaderTests
{
    [Fact]
    public async Task CallsAction()
    {
        var reader = new ActionInputReader<string>(() => "My input");

        var input = await reader.ReadAsync();

        Assert.Equal("My input", input);
    }

    [Fact]
    public async Task NullDoesNothing()
    {
        var reader = ActionInputReader<string>.Null;

        var input = await reader.ReadAsync();

        Assert.Null(input);
    }
}
