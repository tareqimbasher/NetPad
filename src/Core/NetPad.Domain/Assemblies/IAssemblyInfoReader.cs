using System.Collections.Generic;

namespace NetPad.Assemblies;

public interface IAssemblyInfoReader
{
    public HashSet<string> GetNamespaces(byte[] assembly);
}
