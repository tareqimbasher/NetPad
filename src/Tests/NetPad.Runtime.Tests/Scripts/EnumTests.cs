using NetPad.Scripts;
using Xunit;

namespace NetPad.Runtime.Tests.Scripts;

public class EnumTests
{
    [Fact]
    public void ScriptKind_AvailableValues()
    {
        var values = Enum.GetNames<ScriptKind>();

        Assert.Equal([
            "Expression",
            "Program",
            "SQL"
        ], values);
    }

    [Fact]
    public void ScriptStatus_AvailableValues()
    {
        var values = Enum.GetNames<ScriptStatus>();

        Assert.Equal([
            "Ready",
            "Running",
            "Stopping",
            "Error"
        ], values);
    }
}
