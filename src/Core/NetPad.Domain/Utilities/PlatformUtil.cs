using System;
using System.Runtime.InteropServices;

namespace NetPad.Utilities;

public static class PlatformUtil
{
    public static OSPlatform GetOSPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return OSPlatform.Linux;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return OSPlatform.OSX;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return OSPlatform.Windows;
        else
            return OSPlatform.FreeBSD;
    }
}
