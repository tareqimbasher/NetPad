using System.Reflection;

namespace NetPad.Apps;

/// <summary>
/// Information about the running web host.
/// </summary>
public class HostInfo
{
    public string HostUrl { get; private set; } = "http://localhost";
    public string WorkingDirectory { get; private set; } = Assembly.GetEntryAssembly()?.Location ?? "./";

    public void SetHostUrl(string url)
    {
        HostUrl = url;
    }

    public void SetWorkingDirectory(string workingDirectory)
    {
        WorkingDirectory = workingDirectory;
    }
}
