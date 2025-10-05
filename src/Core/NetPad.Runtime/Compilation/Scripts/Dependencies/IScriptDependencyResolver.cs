using NetPad.Scripts;

namespace NetPad.Compilation.Scripts.Dependencies;

public interface IScriptDependencyResolver
{
    /// <summary>
    /// Gets all dependencies a script needs to run.
    /// </summary>
    Task<ScriptDependencies> GetDependenciesAsync(Script script, CancellationToken cancellationToken);
}
