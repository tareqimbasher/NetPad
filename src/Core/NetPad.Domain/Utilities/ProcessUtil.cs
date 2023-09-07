using System;
using System.Diagnostics;

namespace NetPad.Utilities;

public static class ProcessUtil
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

    public static void OpenInDesktopExplorer(string path)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    public static ProcessStartInfo CopyCurrentEnvironmentVariables(this ProcessStartInfo processStartInfo, bool overwrite = true)
    {
        var envVars = Environment.GetEnvironmentVariables();

        foreach (string key in envVars.Keys)
        {
            if (!overwrite && processStartInfo.EnvironmentVariables.ContainsKey(key)) continue;

            processStartInfo.EnvironmentVariables[key] = envVars[key]?.ToString();
        }

        return processStartInfo;
    }
}
