using System.Diagnostics;

namespace NetPad.DotNet;

public class DotNetProcessRunner
{
    public string ExecuteCommand(string fileName, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        }.CopyCurrentEnvironmentVariables());

        if (process == null)
            throw new Exception($"Failed to start process: {fileName}");

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }
}
