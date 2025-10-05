namespace NetPad.Compilation.Scripts.Dependencies;

/// <summary>
/// All dependencies required by a script to run.
/// </summary>
public class ScriptDependencies(List<ReferenceDependency> referenceDependencies, List<CodeDependency> codeDependencies)
{
    /// <summary>
    /// List of references a script needs to run.
    /// </summary>
    public List<ReferenceDependency> References { get; private set; } = referenceDependencies;

    /// <summary>
    /// List of code that should be added to a script for it to run.
    /// </summary>
    public List<CodeDependency> Code { get; private set; } = codeDependencies;
}
