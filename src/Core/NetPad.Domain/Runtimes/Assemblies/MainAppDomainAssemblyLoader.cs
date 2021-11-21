using System;
using System.Reflection;

namespace NetPad.Runtimes.Assemblies
{
    public class MainAppDomainAssemblyLoader : IAssemblyLoader
    {
        public Assembly LoadFrom(byte[] assemblyBytes)
        {
            return AppDomain.CurrentDomain.Load(
                assemblyBytes ?? throw new ArgumentNullException(nameof(assemblyBytes)));
        }

        public void UnloadLoadedAssemblies()
        {
            // We cannot unload from main app domain
        }

        public void Dispose()
        {
            UnloadLoadedAssemblies();
        }
    }
}
