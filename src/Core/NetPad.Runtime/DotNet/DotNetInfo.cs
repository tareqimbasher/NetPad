using System.Diagnostics;
using System.IO;
using NetPad.Configuration;

namespace NetPad.DotNet;

public class DotNetInfo(Settings settings) : IDotNetInfo
{
    private readonly DotNetEnvironment _environment = new();
    private readonly DotNetProcessRunner _processRunner = new();
    private readonly DotNetPathResolver _pathResolver = new();

    private readonly object _dotNetRootDirLocateLock = new();
    private string? _dotNetRootDirPath;

    private readonly object _dotNetExeLocateLock = new();
    private string? _dotNetExecutablePath;

    private readonly object _dotNetEfToolExeLocateLock = new();
    private string? _dotNetEfToolPath;

    private static readonly object _dotNetRuntimeVersionsLocateLock = new();
    private static DotNetRuntimeVersion[]? _dotNetRuntimeVersions;

    private static readonly string _dotNetExeName = PlatformUtil.IsOSWindows() ? "dotnet.exe" : "dotnet";
    private static readonly object _dotNetSdkVersionsLocateLock = new();
    private static DotNetSdkVersion[]? _dotNetSdkVersions;

    /// <summary>
    /// Returns the version of the .NET runtime used in the current app domain.
    /// </summary>
    public SemanticVersion GetCurrentDotNetRuntimeVersion() => _environment.GetCurrentDotNetRuntimeVersion();


    public string LocateDotNetRootDirectoryOrThrow()
    {
        return LocateDotNetRootDirectory() ?? throw new Exception("Could not find the dotnet ROOT directory.");
    }

    public string? LocateDotNetRootDirectory()
    {
        if (_dotNetRootDirPath != null)
        {
            return _dotNetRootDirPath;
        }

        lock (_dotNetRootDirLocateLock)
        {
            if (_dotNetRootDirPath != null)
            {
                return _dotNetRootDirPath;
            }

            var report = _pathResolver.FindDotNetInstallDir(settings);

            if (report.ResolvedPath is not null)
            {
                Environment.SetEnvironmentVariable("DOTNET_ROOT", report.ResolvedPath.Path);
            }

            _dotNetRootDirPath = report.ResolvedPath?.Path;
        }

        return _dotNetRootDirPath;
    }


    public string LocateDotNetExecutableOrThrow()
    {
        var path = LocateDotNetExecutable();

        if (path != null) return path;

        throw new Exception($"Could not find the '{_dotNetExeName}' executable. " +
                            $"Verify that '{_dotNetExeName}' is in your PATH, or ensure the 'DOTNET_ROOT' environment variable is set.");
    }

    public string? LocateDotNetExecutable()
    {
        if (_dotNetExecutablePath != null)
        {
            return _dotNetExecutablePath;
        }

        lock (_dotNetExeLocateLock)
        {
            if (_dotNetExecutablePath != null)
            {
                return _dotNetExecutablePath;
            }

            string? exePath = null;

            var rootDirPath = LocateDotNetRootDirectory();

            if (DotNetPathResolver.IsValidDotNetSdkRootDirectory(rootDirPath))
            {
                exePath = Path.Combine(rootDirPath!, _dotNetExeName);
            }

            _dotNetExecutablePath = exePath;
        }

        return _dotNetExecutablePath;
    }


    public DotNetRuntimeVersion[] GetDotNetRuntimeVersionsOrThrow()
    {
        var versions = GetDotNetRuntimeVersions();

        return versions.Length > 0
            ? versions
            : throw new Exception("Could not find any .NET runtimes");
    }

    public DotNetRuntimeVersion[] GetDotNetRuntimeVersions()
    {
        if (_dotNetRuntimeVersions?.Any() == true)
        {
            return _dotNetRuntimeVersions;
        }

        lock (_dotNetRuntimeVersionsLocateLock)
        {
            if (_dotNetRuntimeVersions?.Any() == true)
            {
                return _dotNetRuntimeVersions;
            }

            var dotNetExePath = LocateDotNetExecutable();
            if (dotNetExePath == null) return [];

            var output = _processRunner.ExecuteCommand(dotNetExePath, "--list-runtimes");

            _dotNetRuntimeVersions = output.Split(Environment.NewLine)
                .Select(l => l.Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x => x.Trim()).ToArray())
                .Where(a => a.Length == 2 && a.All(x => x.Any()))
                .Select(a => SemanticVersion.TryParse(a[1], out var version) ? new DotNetRuntimeVersion(a[0], version) : null)
                .Where(v => v != null)
                .ToArray()!;
        }

        return _dotNetRuntimeVersions;
    }


    public DotNetSdkVersion[] GetDotNetSdkVersionsOrThrow()
    {
        var versions = GetDotNetSdkVersions();

        return versions.Length > 0
            ? versions
            : throw new Exception("Could not find any .NET SDKs");
    }

    public DotNetSdkVersion[] GetDotNetSdkVersions()
    {
        if (_dotNetSdkVersions?.Any() == true)
        {
            return _dotNetSdkVersions;
        }

        lock (_dotNetSdkVersionsLocateLock)
        {
            if (_dotNetSdkVersions?.Any() == true)
            {
                return _dotNetSdkVersions;
            }

            var dotNetExePath = LocateDotNetExecutable();
            if (dotNetExePath == null) return [];

            var output = _processRunner.ExecuteCommand(dotNetExePath, "--list-sdks");

            _dotNetSdkVersions = output.Split(Environment.NewLine)
                .Select(l => l.Split(" ")[0].Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => SemanticVersion.TryParse(v, out var version) ? new DotNetSdkVersion(version) : null)
                .Where(x => x is not null)
                .ToArray()!;
        }

        return _dotNetSdkVersions;
    }

    public DotNetSdkVersion GetLatestSupportedDotNetSdkVersionOrThrow(bool includePrerelease = false)
    {
        var latestSupported = GetLatestSupportedDotNetSdkVersion(includePrerelease);

        return latestSupported ?? throw new Exception("Could not find any supported .NET SDKs");
    }

    public DotNetSdkVersion? GetLatestSupportedDotNetSdkVersion(bool includePrerelease = false)
    {
        return GetDotNetSdkVersions()
            .Where(v => v.IsSupported() && (includePrerelease || !v.Version.IsPrerelease))
            .MaxBy(x => x.Version);
    }


    public string LocateDotNetEfToolExecutableOrThrow()
    {
        var path = LocateDotNetEfToolExecutable();

        if (path != null) return path;

        var exeName = GetDotNetEfToolExeName();
        throw new Exception($"Could not find the '{exeName}' executable. " +
                            $"Verify that '{exeName}' is in your PATH, or ensure the that you have it installed in " +
                            "the dotnet global tools path under '{UserHomeDirectory}/.dotnet/tools'.");
    }

    public string? LocateDotNetEfToolExecutable()
    {
        if (_dotNetEfToolPath != null)
        {
            return _dotNetEfToolPath;
        }

        lock (_dotNetEfToolExeLocateLock)
        {
            if (_dotNetEfToolPath != null)
            {
                return _dotNetEfToolPath;
            }

            string? path = null;
            var exeName = GetDotNetEfToolExeName();

            try
            {
                // Try getting path using ShellExecute
                // Prioritize this over global tool install path in case user defines a different path for dotnet for the execution of this app.
                using var process = Process.Start(new ProcessStartInfo
                {
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = exeName,
                    Arguments = "--version",
                    RedirectStandardOutput = true
                }.CopyCurrentEnvironmentVariables());

                path = process?.MainModule?.FileName;

                // Process file path could sometimes point to the shell that executed the command, ex: if ShellExecute could find the command
                if (path?.EndsWith(exeName) != true)
                {
                    path = null;
                }
            }
            catch
            {
                // if it failed, it wasn't found
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                // Try getting path from global tool install path
                var globalToolInstallPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".dotnet",
                    "tools"
                );

                var testPath = Path.Combine(globalToolInstallPath, exeName);

                if (File.Exists(testPath))
                {
                    path = testPath;
                }
            }

            _dotNetEfToolPath = path;
        }

        return _dotNetEfToolPath;
    }

    public SemanticVersion? GetDotNetEfToolVersion(string dotNetEfToolExePath)
    {
        var output = _processRunner.ExecuteCommand(dotNetEfToolExePath, "--version");
        return SemanticVersion.TryParse(output.Split(Environment.NewLine).Skip(1).FirstOrDefault(), out var version)
            ? version
            : null;
    }

    private static string GetDotNetEfToolExeName()
    {
        return PlatformUtil.IsOSWindows() ? "dotnet-ef.exe" : "dotnet-ef";
    }
}
