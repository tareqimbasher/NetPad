using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using NetPad.Utilities;

namespace NetPad.Runtimes.Assemblies
{
    public sealed class UnloadableAssemblyLoader : AssemblyLoadContext, IAssemblyLoader
    {
        private readonly ILogger<UnloadableAssemblyLoader> _logger;
        private bool _unloaded;
        private readonly Dictionary<string, ReferencedAssembly> _referenceAssemblies;

        public UnloadableAssemblyLoader(ILogger<UnloadableAssemblyLoader> logger) : base(isCollectible: true)
        {
            _logger = logger;
            _referenceAssemblies = new Dictionary<string, ReferencedAssembly>();
        }

        public UnloadableAssemblyLoader(IEnumerable<string> referenceAssemblyPaths, ILogger<UnloadableAssemblyLoader> logger) : this(logger)
        {
            _referenceAssemblies = referenceAssemblyPaths
                .Select(p => new ReferencedAssembly(p, true))
                .ToDictionary(k => k.AssemblyName, v => v);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (_referenceAssemblies.TryGetValue(assemblyName.FullName, out var assembly))
                return LoadFrom(assembly.Bytes);

            foreach (var referenceAssembly in _referenceAssemblies.Values)
            {
                if (referenceAssembly.AssembliesInSameDir?.TryGetValue(assemblyName.FullName, out var r) == true)
                    return LoadFrom(r!.Bytes);
            }

            return base.Load(assemblyName);
        }

        public Assembly LoadFrom(byte[] assemblyBytes)
        {
            _logger.LogTrace($"{nameof(LoadFrom)} start");

            try
            {
                if (_unloaded)
                    throw new InvalidOperationException("Assemblies have been unloaded. You cannot load a new assembly");

                // Checkout: https://github.com/natemcmaster/DotNetCorePlugins
                using var stream = new MemoryStream(assemblyBytes);
                var assembly = LoadFromStream(stream);
                return assembly;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading assembly bytes");
                throw;
            }
            finally
            {
                _logger.LogTrace($"{nameof(LoadFrom)} end");
            }
        }

        public void UnloadLoadedAssemblies()
        {
            _logger.LogTrace($"{nameof(UnloadLoadedAssemblies)} start");

            try
            {
                if (_unloaded)
                {
                    _logger.LogWarning("AssemblyLoadContext is unloaded, yet a call was made to unload");
                    return;
                }

                Unload();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in unload");
            }
            finally
            {
                _unloaded = true;
                _logger.LogTrace($"{nameof(UnloadLoadedAssemblies)} end");
            }
        }

        public void Dispose()
        {
            _logger.LogTrace($"{nameof(Dispose)} start");
            UnloadLoadedAssemblies();
            GCUtil.CollectAndWait();
            _logger.LogTrace($"{nameof(Dispose)} end ");
        }


        public class ReferencedAssembly
        {
            private byte[]? _bytes;

            public ReferencedAssembly(string path, bool scanOtherAssembliesInSameDir)
            {
                Path = path ?? throw new ArgumentNullException(nameof(path));
                AssemblyName = System.Reflection.AssemblyName.GetAssemblyName(path).FullName;

                if (scanOtherAssembliesInSameDir)
                {
                    AssembliesInSameDir = Directory.GetFiles(System.IO.Path.GetDirectoryName(path)!, "*.dll")
                        .Select(dll => new ReferencedAssembly(dll, false))
                        .ToDictionary(k => k.AssemblyName, v => v);
                }
            }

            public string Path { get; }
            public string AssemblyName { get; }
            public byte[] Bytes => _bytes ??= File.ReadAllBytes(Path);
            public Dictionary<string, ReferencedAssembly>? AssembliesInSameDir { get; }
        }
    }
}
