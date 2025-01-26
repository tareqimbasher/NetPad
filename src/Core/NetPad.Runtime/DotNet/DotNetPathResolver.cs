using System.IO;

namespace NetPad.DotNet;

public class DotNetPathResolver
{
    private static readonly string _dotNetExeName = PlatformUtil.IsOSWindows() ? "dotnet.exe" : "dotnet";

    public string? SearchCommonLocationsForDotNetRootDirectory()
    {
        var possibleDirectories = new List<string?>
        {
            // Give the highest priority to env variables
            Environment.GetEnvironmentVariable("DOTNET_ROOT"),
            Environment.GetEnvironmentVariable("DOTNET_INSTALL_DIR")
        };

        // Common installation paths in descending priority
        if (PlatformUtil.IsOSWindows())
        {
            possibleDirectories.Add(@"C:\Program Files\dotnet\x64");
            possibleDirectories.Add(@"C:\Program Files\dotnet");
        }
        else
        {
            possibleDirectories.Add("/usr/local/share/dotnet"); // default for macOS
            possibleDirectories.Add("/usr/share/dotnet");
            possibleDirectories.Add("/usr/lib/dotnet");
            possibleDirectories.Add("/usr/lib64/dotnet");
            possibleDirectories.Add("/opt/dotnet");
        }

        possibleDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet"));

        return possibleDirectories.FirstOrDefault(IsValidDotNetSdkRootDirectory);
    }

    public bool IsValidDotNetSdkRootDirectory(string? path)
    {
        if (path == null || !Directory.Exists(path))
        {
            return false;
        }

        // Confirm the directory has the dotnet executable
        var exePath = Path.Combine(path, _dotNetExeName);
        return File.Exists(exePath);
    }
}
