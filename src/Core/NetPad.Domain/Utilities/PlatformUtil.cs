using System;
using System.Runtime.InteropServices;

namespace NetPad.Utilities;

public static class PlatformUtil
{
    public static OSPlatform GetOSPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return OSPlatform.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return OSPlatform.OSX;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return OSPlatform.Linux;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            return OSPlatform.FreeBSD;

        throw new Exception($"Could not determine OS platform. OS: {RuntimeInformation.OSDescription}");
    }

    public static bool IsWindowsPlatform() => GetOSPlatform() == OSPlatform.Windows;
}
