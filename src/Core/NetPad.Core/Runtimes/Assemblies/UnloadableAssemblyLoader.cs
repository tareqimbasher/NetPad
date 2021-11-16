using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace NetPad.Runtimes.Assemblies
{
    public sealed class UnloadableAssemblyLoader : AssemblyLoadContext, IAssemblyLoader
    {
        public UnloadableAssemblyLoader() : base(isCollectible: true)
        {
        }

        public Assembly LoadFrom(byte[] assemblyBytes)
        {
            // Checkout: https://github.com/natemcmaster/DotNetCorePlugins
            using var stream = new MemoryStream(assemblyBytes);
            return base.LoadFromStream(stream);
        }

        public void UnloadLoadedAssemblies()
        {
            Unload();
        }

        public void Dispose()
        {
            UnloadLoadedAssemblies();
        }
    }
}
