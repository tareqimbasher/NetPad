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
        using var p = Process.Start(new ProcessStartInfo
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

    public static void MakeExecutable(string filePath)
    {
        if (PlatformUtil.IsOSWindows())
        {
            return;
        }

        using var p = Process.Start("chmod", $"+x {filePath}");

        p.WaitForExit();
    }

    public static ProcessStartInfo WithWorkingDirectory(this ProcessStartInfo processStartInfo, string workingDirectory)
    {
        processStartInfo.WorkingDirectory = workingDirectory;
        return processStartInfo;
    }

    public static ProcessStartInfo WithRedirectIO(this ProcessStartInfo processStartInfo)
    {
        processStartInfo.RedirectStandardInput = true;
        processStartInfo.RedirectStandardOutput = true;
        processStartInfo.RedirectStandardError = true;

        return processStartInfo;
    }

    public static ProcessStartInfo WithNoUi(this ProcessStartInfo processStartInfo)
    {
        processStartInfo.CreateNoWindow = true;
        processStartInfo.UseShellExecute = false;
        processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;

        return processStartInfo;
    }
}
