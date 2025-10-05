using NetPad.Scripts;

namespace NetPad.Compilation.Scripts;

/// <summary>
/// A high-level abstraction that compiles a script into an assembly that is ready to run.
/// </summary>
public interface IScriptCompiler
{
    /// <summary>
    /// Parses a script's source code and compiles it, and emits an assembly that is ready to run.
    /// </summary>
    /// <param name="code">
    /// The code to compile.
    /// <remarks>
    /// The value passed here is typically the entire source code of the script. But in some cases, we just want
    /// to compile a part of the script's source code. For example if the user highlights one statement in their
    /// script and hit "Run" they only want to include that line. So this parameter is here for that purpose.
    /// </remarks>
    /// </param>
    /// <param name="script">The target script.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ParseAndCompileResult?> ParseAndCompileAsync(
        string code,
        Script script,
        CancellationToken cancellationToken);
}
