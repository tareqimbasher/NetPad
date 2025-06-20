using NetPad.ExecutionModel;
using NetPad.IO;

namespace NetPad.Tests.Services;

public class NullScriptRunner : IScriptRunner
{
    public void Dispose()
    {
    }

    public Task<RunResult> RunScriptAsync(RunOptions runOptions)
    {
        return Task.FromResult(RunResult.RunAttemptFailure());
    }

    public Task StopScriptAsync()
    {
        return Task.CompletedTask;
    }

    public string[] GetUserVisibleAssemblies()
    {
        return [];
    }

    public void AddInput(IInputReader<string> inputReader)
    {
    }

    public void RemoveInput(IInputReader<string> inputReader)
    {
    }

    public void AddOutput(IOutputWriter<object> outputWriter)
    {
    }

    public void RemoveOutput(IOutputWriter<object> outputWriter)
    {
    }

    public void DumpMemCacheItem(string key)
    {
    }

    public void DeleteMemCacheItem(string key)
    {
    }

    public void ClearMemCacheItems()
    {
    }
}
