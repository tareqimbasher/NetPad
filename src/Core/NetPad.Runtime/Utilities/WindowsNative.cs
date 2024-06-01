using System.Runtime.InteropServices;

namespace NetPad.Utilities;

public static class WindowsNative
{
    public static void DisableWindowsErrorReporting()
    {
        // If older than Window 7
        if (Environment.OSVersion.Version < new Version(6, 1, 0, 0))
        {
            return;
        }

        SetErrorMode(GetErrorMode() |
                     ErrorMode.SEM_FAILCRITICALERRORS |
                     ErrorMode.SEM_NOOPENFILEERRORBOX |
                     ErrorMode.SEM_NOGPFAULTERRORBOX);
    }

    [DllImport("kernel32", PreserveSig = true)]
    private static extern ErrorMode SetErrorMode(ErrorMode mode);

    [DllImport("kernel32", PreserveSig = true)]
    private static extern ErrorMode GetErrorMode();

    [Flags]
    private enum ErrorMode
    {
        SYSTEM_DEFAULT = 0x0,
        SEM_FAILCRITICALERRORS = 0x0001,
        SEM_NOGPFAULTERRORBOX = 0x0002,
        SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,
        SEM_NOOPENFILEERRORBOX = 0x8000,
    }
}
