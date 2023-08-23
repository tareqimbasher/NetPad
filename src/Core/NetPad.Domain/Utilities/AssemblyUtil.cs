using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

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

    /// <summary>
    /// Determines if a file is an assembly.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <remarks>
    /// See: https://learn.microsoft.com/en-us/dotnet/standard/assembly/identify
    /// </remarks>
    public static bool IsAssembly(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            using var peReader = new PEReader(fs);

            if (!peReader.HasMetadata)
            {
                return false;
            }

            // Check that file has an assembly manifest.
            MetadataReader reader = peReader.GetMetadataReader();
            return reader.IsAssembly;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
    }
}
