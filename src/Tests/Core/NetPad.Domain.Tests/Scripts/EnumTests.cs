using System;
using NetPad.Scripts;
using Xunit;

namespace NetPad.Domain.Tests.Scripts;

public class EnumTests
{
    [Fact]
    public void ScriptKind_AvailableValues()
    {
        var values = Enum.GetNames<ScriptKind>();

        Assert.Equal(new[]
        {
            "Expression",
            "Program"
        }, values);
    }

    [Fact]
    public void ScriptStatus_AvailableValues()
    {
        var values = Enum.GetNames<ScriptStatus>();

        Assert.Equal(new[]
        {
            "Ready",
            "Running",
            "Stopping",
            "Error"
        }, values);
    }
}
