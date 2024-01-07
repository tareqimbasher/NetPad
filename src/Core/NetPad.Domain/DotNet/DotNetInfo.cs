using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NetPad.Configuration;

namespace NetPad.DotNet;

public class DotNetInfo : IDotNetInfo
{
    private static readonly SemanticVersion _environmentVersion = new(Environment.Version);
    private readonly Settings _settings;

    private readonly object _dotNetRootDirLocateLock = new();
    private string? _dotNetRootDirPath;

    private readonly object _dotNetExeLocateLock = new();
    private string? _dotNetExecutablePath;

    private readonly object _dotNetEfToolExeLocateLock = new();
    private string? _dotNetEfToolPath;

    private static readonly object _dotNetRuntimeVersionsLocateLock = new();
    private static DotNetRuntimeVersion[]? _dotNetRuntimeVersions;

    private static readonly object _dotNetSdkVersionsLocateLock = new();
    private static DotNetSdkVersion[]? _dotNetSdkVersions;

    public DotNetInfo(Settings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Returns the version of the .NET runtime used in the current app domain.
    /// </summary>
    public SemanticVersion GetCurrentDotNetRuntimeVersion() => _environmentVersion;


    public string LocateDotNetRootDirectoryOrThrow()
    {
        return LocateDotNetRootDirectory() ?? throw new Exception("Could not find the dotnet ROOT directory.");
    }

    public string? LocateDotNetRootDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_settings.DotNetSdkDirectoryPath))
        {
            return IsValidDotNetSdkRootDirectory(_settings.DotNetSdkDirectoryPath)
                ? _settings.DotNetSdkDirectoryPath
                : null;
        }

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

            string? rootDirPath = null;

            var exePathFromShell = GetDotNetExePathFromShell();

            if (exePathFromShell != null)
            {
                rootDirPath = Path.GetDirectoryName(exePathFromShell);
                if (!IsValidDotNetSdkRootDirectory(rootDirPath))
                {
                    rootDirPath = null;
                }
            }

            if (rootDirPath == null)
            {
                rootDirPath = SearchCommonLocationsForDotNetRootDirectory();
                if (!IsValidDotNetSdkRootDirectory(rootDirPath))
                {
                    rootDirPath = null;
                }
            }

            if (rootDirPath != null)
            {
                Environment.SetEnvironmentVariable("DOTNET_ROOT", rootDirPath);
            }

            _dotNetRootDirPath = rootDirPath;
        }

        return _dotNetRootDirPath;
    }


    public string LocateDotNetExecutableOrThrow()
    {
        var path = LocateDotNetExecutable();

        if (path != null) return path;

        var exeName = GetDotNetExeName();
        throw new Exception($"Could not find the '{exeName}' executable. " +
                            $"Verify that '{exeName}' is in your PATH, or ensure the 'DOTNET_ROOT' environment variable is set.");
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

            if (IsValidDotNetSdkRootDirectory(rootDirPath))
            {
                var exeName = GetDotNetExeName();
                exePath = Path.Combine(rootDirPath!, exeName);
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
            if (dotNetExePath == null) return Array.Empty<DotNetRuntimeVersion>();

            using var p = Process.Start(new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = dotNetExePath,
                Arguments = "--list-runtimes",
                RedirectStandardOutput = true
            });

            if (p == null)
                throw new Exception("Could not start dotnet sdk executable");

            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

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
            if (dotNetExePath == null) return Array.Empty<DotNetSdkVersion>();

            using var p = Process.Start(new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = dotNetExePath,
                Arguments = "--list-sdks",
                RedirectStandardOutput = true
            });

            if (p == null)
                throw new Exception("Could not start dotnet sdk executable");

            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            _dotNetSdkVersions = output.Split(Environment.NewLine)
                .Select(l => l.Split(" ")[0].Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => SemanticVersion.TryParse(v, out var version) ? new DotNetSdkVersion(version) : null)
                .Where(x => x is not null)
                .ToArray()!;
        }

        return _dotNetSdkVersions;
    }

    public DotNetSdkVersion GetLatestSupportedDotNetSdkVersionOrThrow()
    {
        var latestSupported = GetLatestSupportedDotNetSdkVersion();

        return latestSupported ?? throw new Exception("Could not find any supported .NET SDKs");
    }

    public DotNetSdkVersion? GetLatestSupportedDotNetSdkVersion()
    {
        return GetDotNetSdkVersions().Where(v => v.IsSupported()).MaxBy(x => x.Version);
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
                    Arguments = "--version"
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
        using var p = Process.Start(new ProcessStartInfo
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            FileName = dotNetEfToolExePath,
            Arguments = "--version",
            RedirectStandardOutput = true
        }.CopyCurrentEnvironmentVariables());

        if (p == null)
            throw new Exception("Could not start EF dotnet tool executable");

        string output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();

        return SemanticVersion.TryParse(output.Split(Environment.NewLine).Skip(1).FirstOrDefault(), out var version)
            ? version
            : null;
    }


    private static string GetDotNetExeName()
    {
        return PlatformUtil.IsWindowsPlatform() ? "dotnet.exe" : "dotnet";
    }

    private static string GetDotNetEfToolExeName()
    {
        return PlatformUtil.IsWindowsPlatform() ? "dotnet-ef.exe" : "dotnet-ef";
    }

    private static bool IsValidDotNetSdkRootDirectory(string? path)
    {
        if (path == null || !Directory.Exists(path))
        {
            return false;
        }

        // Confirm the directory has the dotnet executable
        var exeName = GetDotNetExeName();
        var exePath = Path.Combine(path, exeName);
        return File.Exists(exePath);
    }

    private static string? GetDotNetExePathFromShell()
    {
        try
        {
            var exeName = GetDotNetExeName();

            // Try getting path using ShellExecute
            using var process = Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = exeName,
                Arguments = "--version"
            }.CopyCurrentEnvironmentVariables());

            var path = process?.MainModule?.FileName;

            // Process file path could sometimes point to the shell that executed the command, ex: if ShellExecute could find the command
            if (path?.EndsWith(exeName) != true)
            {
                return null;
            }

            return path;
        }
        catch
        {
            // If it failed, it wasn't found
            return null;
        }
    }

    private static string? SearchCommonLocationsForDotNetRootDirectory()
    {
        var possibleDirectories = new List<string?>();

        // Give highest priority to env variables
        possibleDirectories.Add(Environment.GetEnvironmentVariable("DOTNET_ROOT"));
        possibleDirectories.Add(Environment.GetEnvironmentVariable("DOTNET_INSTALL_DIR"));

        // Common installation paths in descending priority
        if (PlatformUtil.IsWindowsPlatform())
        {
            possibleDirectories.Add(@"C:\Program Files\dotnet\x64");
            possibleDirectories.Add(@"C:\Program Files\dotnet");
        }
        else
        {
            possibleDirectories.Add("/usr/local/share/dotnet"); // default for macOS
            possibleDirectories.Add("/usr/share/dotnet");
            possibleDirectories.Add("/usr/lib/dotnet");
            possibleDirectories.Add("/opt/dotnet");
        }

        possibleDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet"));

        foreach (var directory in possibleDirectories)
        {
            if (IsValidDotNetSdkRootDirectory(directory))
            {
                return directory;
            }
        }

        return null;
    }
}
