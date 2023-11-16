using System.Text.Json;
using NetPad.IO;
using Xunit;

namespace NetPad.Runtimes.Tests.ExternalProcessScriptRuntime;

public class ScriptRuntimeServicesTests
{
    [Fact]
    public void SetIO_Works()
    {
        var testResultOutput = new HtmlResultsScriptOutput("test result output");

        ScriptRuntimeServices.SetIO(new ActionOutputWriter<object>((o, t) => Assert.Equal(testResultOutput, o)));

        ScriptRuntimeServices.ResultWrite(testResultOutput);
    }

    [Fact]
    public void DefaultResultOutput_ShouldBeHtml()
    {
        var o = new
        {
            FirstName = "John",
            Age = 21
        };

        string? output = null;
        ScriptRuntimeServices.SetIO(new ActionOutputWriter<object>((o, t) => output = o as string));
        ScriptRuntimeServices.ResultWrite(o);

        Assert.NotNull(output);
        var json = JsonDocument.Parse(output).RootElement;
        Assert.Equal(nameof(HtmlResultsScriptOutput), json.GetProperty(nameof(ExternalProcessOutput.Type).ToLowerInvariant()).GetString());
        Assert.NotEmpty(json.GetProperty(nameof(ExternalProcessOutput.Output).ToLowerInvariant()).ToString());
    }

    [Fact]
    public void DefaultSqlOutput_ShouldBeHtml()
    {
        var o = new
        {
            FirstName = "John",
            Age = 21
        };

        string? output = null;
        ScriptRuntimeServices.SetIO(new ActionOutputWriter<object>((o, t) => output = o as string));
        ScriptRuntimeServices.SqlWrite(o);

        Assert.NotNull(output);
        var json = JsonDocument.Parse(output).RootElement;
        Assert.Equal(nameof(HtmlSqlScriptOutput), json.GetProperty(nameof(ExternalProcessOutput.Type).ToLowerInvariant()).GetString());
        Assert.NotEmpty(json.GetProperty(nameof(ExternalProcessOutput.Output).ToLowerInvariant()).ToString());
    }
}



