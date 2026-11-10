using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using NetPad.Assemblies;
using NetPad.ExecutionModel.ScriptServices;
using NetPad.IO;

namespace NetPad.ExecutionModel.ClientServer;

/// <summary>
/// Runs a compiled script assembly in its own collectible <see cref="AssemblyLoadContext"/>.
/// </summary>
public static class ScriptAssemblyExecutor
{
    /// <summary>
    /// Loads the script assembly in a new collectible context, invokes its entry point, and unloads the context.
    /// </summary>
    /// <param name="scriptAssemblyPath">The path of the compiled script assembly.</param>
    /// <param name="probingPaths">Directories to search for the script's dependencies.</param>
    /// <exception cref="TargetInvocationException">The script threw; the script's exception is the inner exception.</exception>
    // The caller collects right after this returns, so the context and the script assembly must be out of scope
    // by then. Inlining into the caller would not guarantee that.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Execute(string scriptAssemblyPath, string[] probingPaths)
    {
        using var context = CreateLoadContext(scriptAssemblyPath, probingPaths);

        InvokeEntryPoint(context, scriptAssemblyPath);
    }

    internal static UnloadableAssemblyLoadContext CreateLoadContext(string scriptAssemblyPath, string[] probingPaths)
    {
        var context = new UnloadableAssemblyLoadContext(scriptAssemblyPath);

        context.UseProbing(probingPaths, [IsNotAlreadyLoadedInDefaultContext]);

        return context;
    }

    internal static void InvokeEntryPoint(UnloadableAssemblyLoadContext context, string scriptAssemblyPath)
    {
        var assembly = context.LoadFromAssemblyPath(scriptAssemblyPath);

        Util.Stopwatch.Restart();

        assembly.EntryPoint!.Invoke(null, [Array.Empty<string>()]);

        Util.Stopwatch.Stop();
    }

    /// <summary>
    /// Prevents loading assemblies from probing paths if they are already loaded in the default
    /// AssemblyLoadContext. Loading the same assembly in both the custom ALC and the default ALC
    /// creates type identity splits that cause TypeLoadExceptions.
    /// </summary>
    private static bool IsNotAlreadyLoadedInDefaultContext(FilePath filePath, AssemblyName assemblyName)
    {
        if (assemblyName.Name == null)
        {
            return true;
        }

        foreach (var loadedAssembly in AssemblyLoadContext.Default.Assemblies)
        {
            if (string.Equals(loadedAssembly.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
