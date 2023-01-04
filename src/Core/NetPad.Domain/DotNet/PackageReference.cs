using NetPad.Exceptions;

namespace NetPad.DotNet;

public class PackageReference : Reference
{
    public PackageReference(string packageId, string title, string version) : base(title)
    {
        PackageId = packageId;
        Version = version;
    }

    public string PackageId { get; }
    public string Version { get; }

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
