using NetPad.Data;

namespace NetPad.Compilation.Scripts.Dependencies;

/// <summary>
/// What a script dependency resolver is doing, reported as each step starts.
/// </summary>
public abstract record ScriptDependencyResolutionProgress
{
    private ScriptDependencyResolutionProgress()
    {
    }

    /// <summary>
    /// The script's data connection is being set up and has reached a stage.
    /// </summary>
    /// <param name="Stage">The setup stage that started.</param>
    public sealed record SettingUpDataConnection(DataConnectionSetupStage Stage) : ScriptDependencyResolutionProgress;

    /// <summary>
    /// The resolver is loading the assets of the script's references and packages.
    /// </summary>
    public sealed record LoadingReferenceAssets : ScriptDependencyResolutionProgress;
}
