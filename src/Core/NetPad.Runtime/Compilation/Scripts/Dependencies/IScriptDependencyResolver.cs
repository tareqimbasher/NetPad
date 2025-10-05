using System.Collections.Immutable;
using NetPad.Scripts;
using O2Html;

namespace NetPad.Compilation.Scripts.Dependencies;

public interface IScriptDependencyResolver
{
    private static readonly ImmutableArray<string> _userVisibleAssemblies =
    [
        typeof(INetPadRuntimeLibMarker).Assembly.Location,
        typeof(HtmlSerializer).Assembly.Location
    ];

    /// <summary>
    /// Gets assemblies that are provided by NetPad itself that users can reference in scripts.
    /// This is different from the assemblies or nuget packages users have added to their scripts.
    /// If we want an assembly that is packaged with NetPad to be accessible to user code, we add it here.
    /// </summary>
    /// <returns>Fully-qualified file paths of all user-visible assemblies.</returns>
    string[] GetUserVisibleAssemblies() => _userVisibleAssemblies.ToArray();

    /// <summary>
    /// Gets all dependencies a script needs to run.
    /// </summary>
    Task<ScriptDependencies> GetDependenciesAsync(Script script, CancellationToken cancellationToken);
}
