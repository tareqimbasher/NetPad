using NetPad.DotNet;
using NetPad.Events;

namespace NetPad.Data.Events;

/// <summary>
/// Published when the generation of a data connection's resources starts a new stage.
/// </summary>
public class DataConnectionResourcesUpdateProgressEvent(
    DataConnection dataConnection,
    DotNetFrameworkVersion targetFrameworkVersion,
    DataConnectionSetupStage stage) : IEvent
{
    public DataConnection DataConnection { get; } = dataConnection;
    public DotNetFrameworkVersion TargetFrameworkVersion { get; } = targetFrameworkVersion;
    public DataConnectionSetupStage Stage { get; } = stage;
    public string StageDescription => Stage.GetDescription();
}
