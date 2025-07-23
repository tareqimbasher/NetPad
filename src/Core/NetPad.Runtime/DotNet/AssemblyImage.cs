using System.IO;
using System.Reflection;
using NJsonSchema.Annotations;

namespace NetPad.DotNet;

/// <summary>
/// An in-memory assembly.
/// </summary>
public class AssemblyImage
{
    public AssemblyImage(AssemblyName assemblyName, byte[] image)
    {
        if (image.Length == 0)
            throw new ArgumentNullException(nameof(image), "Image has zero length");

        AssemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
        Image = image;
    }

    public AssemblyImage(string assemblyFilePath)
    {
        AssemblyName = AssemblyName.GetAssemblyName(assemblyFilePath);
        Image = File.ReadAllBytes(assemblyFilePath);
    }

    [JsonSchemaIgnore] public AssemblyName AssemblyName { get; }
    [JsonSchemaIgnore] public byte[] Image { get; }

    public string ConstructAssemblyFileName()
    {
        var assemblyName = AssemblyName;

        var fileName = assemblyName.Name ?? assemblyName.FullName.Split(',')[0];

        foreach (var invalidFileNameChar in Path.GetInvalidFileNameChars())
        {
            if (fileName.Contains(invalidFileNameChar))
                fileName = fileName.Replace(invalidFileNameChar.ToString(), "");
        }

        fileName = fileName.Trim();

        if (!fileName.EndsWithIgnoreCase(".dll"))
            fileName += ".dll";

        return fileName;
    }

    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
        {
            int hash = 17;
            hash = hash * 23 + AssemblyName.FullName.GetHashCode();
            hash = hash * 23 + Image.GetHashCode();
            return hash;
        }
    }
}
