using System;
using System.Diagnostics;

namespace NetPad.Utilities;

public static class ProcessUtils
{
    public static bool WasProcessStarted(this Process process)
    {
        try
        {
            _ = process.HasExited;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static bool IsProcessRunning(this Process process)
    {
        return process.WasProcessStarted() && !process.HasExited;
    }
}
