using System.Diagnostics;
using System.IO;
using NetPad.Configuration;
using NetPad.IO;

namespace NetPad.DotNet;

/// <summary>
/// Resolves the path where .NET is installed.
/// </summary>
public class DotNetPathResolver
{
    private static readonly string _dotNetExeName = PlatformUtil.IsOSWindows() ? "dotnet.exe" : "dotnet";

    public DotNetPathReport FindDotNetInstallDir(Settings settings)
    {
        var report = new DotNetPathReport();

        if (!string.IsNullOrWhiteSpace(settings.DotNetSdkDirectoryPath))
        {
            report.AddSearchStep(settings.DotNetSdkDirectoryPath);

            // If user has explicitly set this setting, we don't do any more checks
            return report;
        }

        GetDotNetPathFromShell(report);
        GetDotNetPathFromCommonLocations(report);
        return report;
    }

    private void GetDotNetPathFromShell(DotNetPathReport report)
    {
        try
        {
            // Try getting path using ShellExecute
            using var process = Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = _dotNetExeName,
                Arguments = "--version",
                RedirectStandardOutput = true
            }.CopyCurrentEnvironmentVariables());

            var exePath = process?.MainModule?.FileName;

            // Process file path could sometimes point to the shell that executed the command, ex: if ShellExecute could find the command
            if (exePath?.EndsWith(_dotNetExeName) != true)
            {
                return;
            }

            var dir = Path.GetDirectoryName(exePath);
            if (dir == null)
            {
                return;
            }

            report.AddSearchStep(dir);
        }
        catch
        {
            // If it failed, it wasn't found
        }
    }

    private void GetDotNetPathFromCommonLocations(DotNetPathReport report)
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

        possibleDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".dotnet"));

        foreach (var dir in possibleDirectories)
        {
            if (string.IsNullOrWhiteSpace(dir))
            {
                continue;
            }

            report.AddSearchStep(dir);
        }
    }

    public static bool IsValidDotNetSdkRootDirectory(string? path)
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

public class DotNetPathReport
{
    public DirectoryPath? ResolvedPath { get; set; }

    public List<SearchStep> SearchSteps { get; } = new();

    public void AddSearchStep(DirectoryPath dir)
    {
        if (SearchSteps.Any(x => x.Location == dir))
        {
            return;
        }

        if (!dir.Exists())
        {
            SearchSteps.Add(new SearchStep(dir, SearchStepResult.DirectoryNotFound));
        }
        else if (!DotNetPathResolver.IsValidDotNetSdkRootDirectory(dir.Path))
        {
            SearchSteps.Add(new SearchStep(dir, SearchStepResult.NoInstallationFound));
        }
        else
        {
            SearchSteps.Add(new SearchStep(dir, SearchStepResult.Valid));
            ResolvedPath = dir;
        }
    }
}

public record SearchStep(DirectoryPath Location, SearchStepResult Result);

public enum SearchStepResult
{
    DirectoryNotFound,
    NoInstallationFound,
    Valid,
}
