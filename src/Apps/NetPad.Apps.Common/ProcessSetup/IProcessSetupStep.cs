namespace NetPad.Apps.ProcessSetup;

/// <summary>
/// Something NetPad applies to its own process when it starts.
/// </summary>
public interface IProcessSetupStep
{
    void ApplyToCurrentProcess();
}
