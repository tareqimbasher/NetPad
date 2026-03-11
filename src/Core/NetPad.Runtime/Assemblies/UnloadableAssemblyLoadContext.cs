using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using NetPad.IO;

namespace NetPad.Assemblies;

/// <summary>
/// Initializes a collectible <see cref="AssemblyLoadContext"/> that will attempt to unload any loaded assemblies
/// when disposed.
/// </summary>
public sealed class UnloadableAssemblyLoadContext() : AssemblyLoadContext(isCollectible: true), IDisposable
{
    private AssemblyDependencyResolver? _resolver;
    private bool _unloaded;
    private IList<string> _probingPaths = [];
    private IList<Func<FilePath, AssemblyName, bool>> _probedAssemblyUseConditions = [];
    private Dictionary<string, string>? _assembliesInProbingPaths;
    private string _file = $"/home/tips/test/asm/{DateTime.Now:h:mm:ss}.log";

    /// <summary>
    /// Initializes a collectible <see cref="AssemblyLoadContext"/> that will attempt to unload any loaded assemblies
    /// when disposed.
    /// </summary>
    /// <param name="mainAssemblyPath">The main assembly this AssemblyLoadContext was created for.
    /// Providing this path will enable this AssemblyLoadContext to find and load assemblies located
    /// in the same directory.
    /// </param>
    public UnloadableAssemblyLoadContext(string mainAssemblyPath) : this()
    {
        _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
        File.AppendAllText(_file, $"mainAssemblyPath={mainAssemblyPath}\n");
    }

    /// <summary>
    /// Adds specified paths to list of locations to scan for assemblies during assembly loading.
    /// </summary>
    /// <param name="probingPaths">Directory paths to probe for assemblies.</param>
    /// <param name="probedAssemblyUseConditions">An assembly must pass all these checks to be loaded from an
    /// assembly found while scanning probe paths. If one of these conditions fail, the assembly will not be
    /// loaded from the probed assembly and will be loaded from the default assembly load context instead.
    /// </param>
    public void UseProbing(
        IList<string> probingPaths,
        IList<Func<FilePath, AssemblyName, bool>>? probedAssemblyUseConditions = null)
    {
        _probingPaths = probingPaths;
        _probedAssemblyUseConditions = probedAssemblyUseConditions ?? [];
        File.AppendAllText(_file, $"probingPaths={string.Join(",", probingPaths)}\n");
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        File.AppendAllText(_file, $"Load={assemblyName}\n");

        // First attempt to resolve assembly using resolver
        var assemblyPath = _resolver?.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            File.AppendAllText(_file, $"   ResolveAssemblyToPath={assemblyPath}\n");
            return LoadFromAssemblyPath(assemblyPath);
        }

        // If not found, try scanning probe paths
        var probedAssembly = SearchForAssemblyInProbingPaths(assemblyName);
        if (probedAssembly != null && File.Exists(probedAssembly))
        {
            File.AppendAllText(_file, $"   probedAssembly={probedAssembly}\n");

            var useProbedAssembly =
                _probedAssemblyUseConditions.Count == 0
                || _probedAssemblyUseConditions.All(condition => condition(probedAssembly, assemblyName));

            if (useProbedAssembly)
            {
                return LoadFromAssemblyPath(probedAssembly);
            }
        }

        // if (assemblyName.Name is not null &&
        //     !assemblyName.Name.Contains("EntityFramework") &&
        //     (assemblyName.Name.StartsWith("System.") ||
        //      assemblyName.Name.StartsWith("Microsoft.") /* maybe refine */))
        //     return null;
        //
        // throw new FileNotFoundException($"Could not resolve {assemblyName} for plugin ALC.");

        File.AppendAllText(_file, $"   not found, loading default={probedAssembly}\n");

        // If still not found, attempt to load from default load context
        return base.Load(assemblyName);
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        File.AppendAllText(_file, $"LoadUnmanagedDll={unmanagedDllName}\n");

        var path = _resolver?.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (path != null)
            return LoadUnmanagedDllFromPath(path);

        return base.LoadUnmanagedDll(unmanagedDllName);
    }

    public Assembly LoadFrom(byte[] assemblyBytes)
    {
        File.AppendAllText(_file, $"LoadFrom={assemblyBytes.Length}\n");

        if (_unloaded)
        {
            throw new InvalidOperationException("Assemblies have been unloaded. You cannot load a new assembly");
        }

        using var stream = new MemoryStream(assemblyBytes);
        var assembly = LoadFromStream(stream);
        return assembly;
    }

    private string? SearchForAssemblyInProbingPaths(AssemblyName assemblyName)
    {
        if (_probingPaths.Count == 0)
        {
            return null;
        }

        if (_assembliesInProbingPaths == null)
        {
            _assembliesInProbingPaths = new Dictionary<string, string>();
            foreach (var probingPath in _probingPaths)
            {
                if (!Directory.Exists(probingPath))
                {
                    continue;
                }

                foreach (var filePath in Directory.GetFiles(probingPath))
                {
                    var name = Try.Run(() => AssemblyName.GetAssemblyName(filePath));
                    if (name != null)
                    {
                        _assembliesInProbingPaths.TryAdd(name.ToString(), filePath);
                    }
                }
            }
        }

        return _assembliesInProbingPaths.TryGetValue(assemblyName.ToString(), out var path) ? path : null;
    }

    public void Dispose()
    {
        try
        {
            if (_unloaded)
            {
                return;
            }

            _resolver = null;
            _probedAssemblyUseConditions.Clear();
            _assembliesInProbingPaths?.Clear();
            Unload();
        }
        finally
        {
            _unloaded = true;
        }
    }
}
