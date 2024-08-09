using NetPad.Scripts;
using Xunit;

namespace NetPad.Runtime.Tests.Scripts;

public class EnumTests
{
    [Fact]
    public void ScriptKind_AvailableValues()
    {
        var values = Enum.GetNames<ScriptKind>();

        IEnumerable<string> expectValues =
        [
            "Expression",
            "Program",
            "SQL"
        ];
        Assert.Equal(expectValues, values);
    }

    [Fact]
    public void ScriptStatus_AvailableValues()
    {
        var values = Enum.GetNames<ScriptStatus>();

        IEnumerable<string> expectValues =
        [
            "Ready",
            "Running",
            "Stopping",
            "Error"
        ];
        Assert.Equal(expectValues, values);
    }
}
