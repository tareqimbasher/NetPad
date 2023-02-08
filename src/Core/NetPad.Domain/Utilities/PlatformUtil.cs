using System.Runtime.InteropServices;

namespace NetPad.Utilities;

public static class PlatformUtil
{
    public static OSPlatform GetOSPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return OSPlatform.Linux;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return OSPlatform.OSX;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return OSPlatform.Windows;
        return OSPlatform.FreeBSD;
    }

    public static bool IsWindowsPlatform() => GetOSPlatform() == OSPlatform.Windows;
}
