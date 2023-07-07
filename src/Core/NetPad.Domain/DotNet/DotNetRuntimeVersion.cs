using System;

namespace NetPad.DotNet;

public record DotNetRuntimeVersion(string FrameworkName, Version Version)
{
    public override string ToString() => $"{FrameworkName} {Version}";
}
