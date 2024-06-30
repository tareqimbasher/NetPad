using NetPad.DotNet;

namespace NetPad.Packages;

public record PackageAsset(string Path) : ReferenceAsset(Path);
