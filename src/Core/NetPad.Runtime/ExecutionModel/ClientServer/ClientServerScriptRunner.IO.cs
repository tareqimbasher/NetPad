using NetPad.IO;

namespace NetPad.ExecutionModel.ClientServer;

public partial class ClientServerScriptRunner
{
    public void AddInput(IInputReader<string> inputReader)
    {
        ArgumentNullException.ThrowIfNull(inputReader);
        _externalInputReaders.Add(inputReader);
    }

    public void RemoveInput(IInputReader<string> inputReader)
    {
        ArgumentNullException.ThrowIfNull(inputReader);
        _externalInputReaders.Remove(inputReader);
    }

    public void AddOutput(IOutputWriter<object> outputWriter)
    {
        ArgumentNullException.ThrowIfNull(outputWriter);
        _externalOutputWriters.Add(outputWriter);
    }

    public void RemoveOutput(IOutputWriter<object> outputWriter)
    {
        ArgumentNullException.ThrowIfNull(outputWriter);
        _externalOutputWriters.Remove(outputWriter);
    }
}
