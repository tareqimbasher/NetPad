using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NetPad.Common;
using NetPad.Configuration;
using NetPad.Utilities;

namespace NetPad.DotNet;

public class DotNetInfo : IDotNetInfo
{
    private readonly Settings _settings;
    private readonly object _dotNetRootDirLocateLock = new();
    private readonly object _dotNetExeLocateLock = new();
    private readonly object _dotNetEfToolExeLocateLock = new();
    private string? _dotNetRootDirPath;
    private string? _dotNetPath;
    private string? _dotNetEfToolPath;

    public DotNetInfo(Settings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Returns the version of the .NET runtime used in the current app domain.
    /// </summary>
    public Version GetCurrentDotNetRuntimeVersion() => Environment.Version;


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
        if (_dotNetPath != null)
        {
            return _dotNetPath;
        }

        lock (_dotNetExeLocateLock)
        {
            if (_dotNetPath != null)
            {
                return _dotNetPath;
            }

            string? exePath = null;

            var rootDirPath = LocateDotNetRootDirectory();

            if (IsValidDotNetSdkRootDirectory(rootDirPath))
            {
                var exeName = GetDotNetExeName();
                exePath = Path.Combine(rootDirPath!, exeName);
            }

            _dotNetPath = exePath;
        }

        return _dotNetPath;
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
        var dotNetExePath = LocateDotNetExecutable();
        if (dotNetExePath == null) return Array.Empty<DotNetRuntimeVersion>();

        var p = Process.Start(new ProcessStartInfo
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

        return output.Split(Environment.NewLine)
            .Select(l => l.Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries).Take(2).ToArray())
            .Where(a => a.Length == 2)
            .Where(a => a.All(x => x.Any()) && int.TryParse(a[1][0].ToString(), out _))
            .Select(a => new DotNetRuntimeVersion(a[0], a[1]))
            .ToArray();
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
        var dotNetExePath = LocateDotNetExecutable();
        if (dotNetExePath == null) return Array.Empty<DotNetSdkVersion>();

        var p = Process.Start(new ProcessStartInfo
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

        return output.Split(Environment.NewLine)
            .Select(l => l.Split(" ")[0])
            .Where(v => v.Any() && int.TryParse(v[0].ToString(), out _))
            .Where(v => v.StartsWith(BadGlobals.DotNetVersion.ToString()))
            .Select(v => new DotNetSdkVersion(v))
            .ToArray();
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
            string? path = null;

            if (_dotNetEfToolPath != null)
            {
                return _dotNetEfToolPath;
            }

            var exeName = GetDotNetEfToolExeName();

            try
            {
                // Try getting path using ShellExecute
                // Prioritize this over global tool install path in case user defines a different path for dotnet for the execution of this app.
                var process = Process.Start(new ProcessStartInfo
                {
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = exeName,
                    Arguments = "--version"
                });

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

            if (string.IsNullOrEmpty(path))
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

    public Version? GetDotNetEfToolVersion(string dotNetEfToolExePath)
    {
        var p = Process.Start(new ProcessStartInfo
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            FileName = dotNetEfToolExePath,
            Arguments = "--version",
            RedirectStandardOutput = true
        });

        if (p == null)
            throw new Exception("Could not start EF dotnet tool executable");

        string output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();

        return Version.TryParse(output.Split(Environment.NewLine).Skip(1).FirstOrDefault(), out var version)
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
            var process = Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = exeName,
                Arguments = "--version"
            });

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
            possibleDirectories.Add(@"/usr/local/share/dotnet"); // default for macOS
            possibleDirectories.Add(@"/usr/share/dotnet");
            possibleDirectories.Add(@"/usr/lib/dotnet");
            possibleDirectories.Add(@"/opt/dotnet");
        }

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
