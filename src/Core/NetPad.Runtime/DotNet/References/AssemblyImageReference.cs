using NetPad.Exceptions;

namespace NetPad.DotNet.References;

/// <summary>
/// An reference to an in-memory assembly.
/// </summary>
public class AssemblyImageReference(AssemblyImage assemblyImage)
    : Reference(assemblyImage.AssemblyName.Name ?? assemblyImage.AssemblyName.FullName)
{
    public AssemblyImage AssemblyImage { get; } = assemblyImage;

    public override void EnsureValid()
    {
        if (AssemblyImage == null)
            throw new InvalidReferenceException(this, $"{nameof(AssemblyImage)} cannot be null.");
    }
}
