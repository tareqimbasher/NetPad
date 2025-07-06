namespace NetPad.Packages;

/// <summary>
/// A unique package identity.
/// </summary>
/// <param name="Id">The package ID.</param>
/// <param name="Version">The package version.</param>
public record PackageIdentity(string Id, string Version);
