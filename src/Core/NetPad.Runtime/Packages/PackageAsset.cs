using NetPad.DotNet;
using NetPad.DotNet.References;

namespace NetPad.Packages;

public record PackageAsset(string Path) : ReferenceAsset(Path);
