using NetPad.DotNet.References;

namespace NetPad.Packages;

/// <summary>
/// A file that is installed as part of a package. It may or may not be an assembly.
/// </summary>
/// <param name="Path">The path of the file.</param>
public record PackageAsset(string Path) : ReferenceAsset(Path);
