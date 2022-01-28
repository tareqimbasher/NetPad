using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace NetPad.Assemblies
{
    public class AssemblyInfoReader : IAssemblyInfoReader
    {
        public string[] GetNamespaces(byte[] assembly)
        {
            var namespaces = new HashSet<string>();

            using var stream = new MemoryStream(assembly);
            using var portableExecutableReader = new PEReader(stream);
            var metadataReader = portableExecutableReader.GetMetadataReader();

            foreach (var typeDefHandle in metadataReader.TypeDefinitions)
            {
                var typeDef = metadataReader.GetTypeDefinition(typeDefHandle);

                if (string.IsNullOrEmpty(metadataReader.GetString(typeDef.Namespace)))
                    continue; // If it's namespace is blank, it's not a user-defined type

                if (!typeDef.Attributes.HasFlag(TypeAttributes.Public))
                    continue;

                namespaces.Add(metadataReader.GetString(typeDef.Namespace));
            }

            return namespaces.ToArray();
        }
    }
}
