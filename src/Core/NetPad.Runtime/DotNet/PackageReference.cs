using NetPad.Exceptions;

namespace NetPad.DotNet;

public class PackageReference(string packageId, string title, string version) : Reference(title)
{
    public string PackageId { get; } = packageId;
    public string Version { get; } = version;

    public override void EnsureValid()
    {
        if (string.IsNullOrWhiteSpace(PackageId))
            throw new InvalidReferenceException(this, $"Package {nameof(PackageId)} is required.");

        if (string.IsNullOrWhiteSpace(Title))
            throw new InvalidReferenceException(this, $"Package {nameof(Title)} is required.");

        if (string.IsNullOrWhiteSpace(Version))
            throw new InvalidReferenceException(this, $"Package {nameof(Version)} is required.");
    }
}
