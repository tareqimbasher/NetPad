using System.IO;
using System.Reflection;

namespace NetPad.DotNet;

public record AssemblyImage
{
    public AssemblyImage(AssemblyName assemblyName, byte[] image)
    {
        if (image.Length == 0)
            throw new ArgumentNullException(nameof(image), "Image has zero length");

        AssemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
        Image = image;
    }

    public AssemblyName AssemblyName { get; }
    public byte[] Image { get; }

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
        return AssemblyName.FullName.GetHashCode();
    }
}
