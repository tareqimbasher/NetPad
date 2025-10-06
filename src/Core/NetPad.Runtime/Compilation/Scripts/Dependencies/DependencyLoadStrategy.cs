namespace NetPad.Compilation.Scripts.Dependencies;

/// <summary>
/// How a dependency is deployed and loaded.
/// </summary>
public enum DependencyLoadStrategy
{
    /// <summary>
    /// Dependency will be copied to output directory and loaded from there.
    /// </summary>
    DeployAndLoad,

    /// <summary>
    /// Dependency will not be copied, and will be loaded from its original location (in-place).
    /// </summary>
    LoadInPlace
}