using System;
using System.Reflection;

namespace NetPad.Runtimes.Assemblies
{
    public interface IAssemblyLoader : IDisposable
    {
        Assembly LoadFrom(byte[] assemblyBytes);
        void UnloadLoadedAssemblies();
    }
}
