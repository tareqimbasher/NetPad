using System;

namespace NetPad.DotNet;

public record DotNetSdkVersion(Version Version)
{
    public override string ToString() => Version.ToString();
}
