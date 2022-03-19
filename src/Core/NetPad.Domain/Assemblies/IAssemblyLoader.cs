using System;
using System.Reflection;

namespace NetPad.Assemblies
{
    public interface IAssemblyLoader : IDisposable
    {
        Assembly LoadFrom(byte[] assemblyBytes);
        void UnloadLoadedAssemblies();
    }
}
