using NetPad.Exceptions;

namespace NetPad.DotNet.References;

/// <summary>
/// A reference to a NuGet package.
/// </summary>
public class PackageReference(string packageId, string title, string version) : Reference(title)
{
    /// <summary>
    /// The unique NuGet package ID.
    /// </summary>
    public string PackageId { get; } = packageId;

    /// <summary>
    /// The version of the reference package.
    /// </summary>
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
