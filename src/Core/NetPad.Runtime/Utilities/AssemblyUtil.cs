using System.IO;
using System.Reflection;

namespace NetPad.Utilities;

public static class AssemblyUtil
{
    public static string ReadEmbeddedResource(Assembly assembly, string name)
    {
        // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
        string? resourcePath = assembly.GetManifestResourceNames()
            .FirstOrDefault(str => str.EndsWith(name));

        if (resourcePath == null)
            throw new Exception($"Could not find embedded resource with name: {name}");

        using Stream? resourceStream = assembly.GetManifestResourceStream(resourcePath);
        if (resourceStream == null)
            throw new Exception($"Could not get embedded resource stream at path: {resourcePath}");

        using StreamReader reader = new(resourceStream);
        return reader.ReadToEnd();
    }
}
