using NetPad.Exceptions;

namespace NetPad.DotNet;

public class AssemblyImageReference : Reference
{
    public AssemblyImageReference(AssemblyImage assemblyImage) : base(assemblyImage.AssemblyName.Name ?? assemblyImage.AssemblyName.FullName ?? "(Unknown)")
    {
        AssemblyImage = assemblyImage;
    }

    public AssemblyImage AssemblyImage { get; }

    public override void EnsureValid()
    {
        if (AssemblyImage == null)
            throw new InvalidReferenceException(this, $"{nameof(AssemblyImage)} cannot be null.");
    }
}
