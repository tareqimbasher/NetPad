using System;
using System.Collections.Generic;
using System.Reflection;

namespace NetPad.Assemblies
{
    public interface IAssemblyLoader : IDisposable
    {
        Assembly LoadFrom(byte[] assemblyBytes);
        IAssemblyLoader WithReferenceAssemblyPaths(IEnumerable<string> referenceAssemblyPaths);
    }
}
