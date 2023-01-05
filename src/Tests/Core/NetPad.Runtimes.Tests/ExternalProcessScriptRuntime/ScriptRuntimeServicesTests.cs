using System;
using System.IO;
using NetPad.Common;
using NetPad.IO;
using Xunit;

namespace NetPad.Runtimes.Tests.ExternalProcessScriptRuntime;

public class ScriptRuntimeServicesTests
{
    public ScriptRuntimeServicesTests()
    {
        ScriptRuntimeServices.SetIO(null); // Reset IO
    }

    [Fact]
    public void SetIO_Works()
    {
        var testResultOutput = new HtmlScriptOutput("test result output");
        var testSqlOutput = new HtmlScriptOutput("test sql output");

        ScriptRuntimeServices.SetIO(new TestScriptOutputAdapter(
            new ActionOutputWriter<object>((o, t) => Assert.Equal(testResultOutput, o)),
            new ActionOutputWriter<object>((o, t) => Assert.Equal(testSqlOutput, o))
        ));

        ScriptRuntimeServices.ResultWrite(testResultOutput);
        ScriptRuntimeServices.SqlWrite(testSqlOutput);
    }

    [Fact]
    public void DefaultResultOutput_ShouldBeHtml()
    {
        var o = new
        {
            FirstName = "John",
            Age = 21
        };

        using var writer = new StringWriter();
        Console.SetOut(writer);
        ScriptRuntimeServices.Init();
        ScriptRuntimeServices.ResultWrite(o);
        var str = writer.ToString();
        var externalProcessOutput = JsonSerializer.Deserialize<ExternalProcessOutput<HtmlScriptOutput>>(str);

        Assert.NotNull(externalProcessOutput);
        Assert.Equal(ExternalProcessOutputChannel.Results, externalProcessOutput!.Channel);
        Assert.NotNull(externalProcessOutput.Output?.Body);
    }

    [Fact]
    public void DefaultSqlOutput_ShouldBeHtml()
    {
        var o = new
        {
            FirstName = "John",
            Age = 21
        };

        using var writer = new StringWriter();
        Console.SetOut(writer);
        ScriptRuntimeServices.Init();
        ScriptRuntimeServices.SqlWrite(o);
        var externalProcessOutput = JsonSerializer.Deserialize<ExternalProcessOutput<HtmlScriptOutput>>(writer.ToString());

        Assert.NotNull(externalProcessOutput);
        Assert.Equal(ExternalProcessOutputChannel.Sql, externalProcessOutput!.Channel);
        Assert.NotNull(externalProcessOutput.Output?.Body);
    }

    private record TestScriptOutputAdapter
        (IOutputWriter<object> ResultsChannel, IOutputWriter<object>? SqlChannel = null) : IScriptOutputAdapter<object, object>;
}



