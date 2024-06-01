using NetPad.ExecutionModel;
using NetPad.Tests;
using Xunit;
using Xunit.Abstractions;

namespace NetPad.Runtime.Tests.Runtimes;

public class RunResultTests(ITestOutputHelper testOutputHelper) : TestBase(testOutputHelper)
{
    [Fact]
    public void SuccessHelper_MarksRunAttemptAsSuccessful()
    {
        var result = RunResult.Success(0);

        Assert.True(result.IsRunAttemptSuccessful);
    }

    [Fact]
    public void SuccessHelper_MarksScriptCompletionAsSuccessful()
    {
        var result = RunResult.Success(0);

        Assert.True(result.IsScriptCompletedSuccessfully);
    }

    [Fact]
    public void SuccessHelper_SetsDuration()
    {
        var result = RunResult.Success(100);

        Assert.Equal(100, result.DurationMs);
    }

    [Fact]
    public void RunAttemptFailureHelper_MarksRunAttemptAsFailure()
    {
        var result = RunResult.RunAttemptFailure();

        Assert.False(result.IsRunAttemptSuccessful);
    }

    [Fact]
    public void RunAttemptFailureHelper_MarksScriptCompletionAsFailure()
    {
        var result = RunResult.RunAttemptFailure();

        Assert.False(result.IsScriptCompletedSuccessfully);
    }

    [Fact]
    public void RunAttemptFailureHelper_SetsDurationTo0()
    {
        var result = RunResult.RunAttemptFailure();

        Assert.Equal(0, result.DurationMs);
    }

    [Fact]
    public void ScriptCompletionFailureHelper_MarksRunAttemptAsSuccessful()
    {
        var result = RunResult.ScriptCompletionFailure(0);

        Assert.True(result.IsRunAttemptSuccessful);
    }

    [Fact]
    public void ScriptCompletionFailureHelper_MarksScriptCompletionAsFailure()
    {
        var result = RunResult.ScriptCompletionFailure(0);

        Assert.False(result.IsScriptCompletedSuccessfully);
    }

    [Fact]
    public void ScriptCompletionFailureHelper_SetsDurationTo0()
    {
        var result = RunResult.ScriptCompletionFailure(0);

        Assert.Equal(0, result.DurationMs);
    }
}
