using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using NetPad.Utilities;

namespace NetPad.Runtimes.Assemblies
{
    public sealed class UnloadableAssemblyLoader : AssemblyLoadContext, IAssemblyLoader
    {
        private readonly ILogger<UnloadableAssemblyLoader> _logger;
        private bool _unloaded = false;

        public UnloadableAssemblyLoader(ILogger<UnloadableAssemblyLoader> logger) : base(isCollectible: true)
        {
            _logger = logger;
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
                var assembly = base.LoadFromStream(stream);
                return assembly;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading assembly bytes. Details: {ex}");
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
                _logger.LogError($"Error in unload: {ex}");
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
    }
}
