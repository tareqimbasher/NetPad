using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NetPad.Common;
using NetPad.Utilities;

namespace NetPad.DotNet;

public static class DotNetInfo
{
    private static readonly object _dotNetExeLocateLock = new();
    private static readonly object _dotNetEfToolExeLocateLock = new();
    private static string? _dotNetPath;
    private static string? _dotNetEfToolPath;

    /// <summary>
    /// Returns the version of the .NET runtime used in the current app domain.
    /// </summary>
    public static Version GetCurrentDotNetRuntimeVersion() => Environment.Version;


    public static string LocateDotNetRootDirectoryOrThrow()
    {
        return LocateDotNetRootDirectory() ?? throw new Exception("Could not find the dotnet ROOT directory.");
    }

    public static string? LocateDotNetRootDirectory()
    {
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT")
                         ?? Environment.GetEnvironmentVariable("DOTNET_INSTALL_DIR")
                         ?? (PlatformUtil.IsWindowsPlatform() ? @"C:\Program Files\dotnet" : "/usr/local/share/dotnet");

        if (Directory.Exists(dotnetRoot)) return dotnetRoot;

        var dotnetExePath = LocateDotNetExecutable();
        if (dotnetExePath != null)
            dotnetRoot = Path.GetDirectoryName(dotnetExePath);

        return !Directory.Exists(dotnetRoot) ? null : dotnetRoot;
    }


    public static string LocateDotNetExecutableOrThrow()
    {
        var path = LocateDotNetExecutable();

        if (path != null) return path;

        var exeName = GetDotNetExeName();
        throw new Exception($"Could not find the '{exeName}' executable. " +
                            $"Verify that '{exeName}' is in your PATH, or ensure the 'DOTNET_ROOT' environment variable is set.");
    }

    public static string? LocateDotNetExecutable()
    {
        if (_dotNetPath != null)
        {
            return _dotNetPath;
        }

        lock (_dotNetExeLocateLock)
        {
            string? path = null;

            if (_dotNetPath != null)
            {
                return _dotNetPath;
            }

            var exeName = GetDotNetExeName();

            try
            {
                // Try getting path using ShellExecute
                // Prioritize this over DOTNET_ROOT environment variable in case user defines a different path for dotnet for the execution of this app.
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
                // Try getting path from environment variable
                var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
                if (dotnetRoot != null)
                {
                    var testPath = Path.Combine(dotnetRoot, exeName);

                    if (File.Exists(testPath))
                    {
                        path = testPath;
                    }
                }
            }

            _dotNetPath = path;
        }

        return _dotNetPath;
    }


    public static DotNetRuntimeVersion[] GetDotNetRuntimeVersionsOrThrow()
    {
        var versions = GetDotNetRuntimeVersions();

        return versions.Length > 0
            ? versions
            : throw new Exception("Could not find any .NET runtimes");
    }

    public static DotNetRuntimeVersion[] GetDotNetRuntimeVersions()
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


    public static DotNetSdkVersion[] GetDotNetSdkVersionsOrThrow()
    {
        var versions = GetDotNetSdkVersions();

        return versions.Length > 0
            ? versions
            : throw new Exception("Could not find any .NET SDKs");
    }

    public static DotNetSdkVersion[] GetDotNetSdkVersions()
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


    public static string LocateDotNetEfToolExecutableOrThrow()
    {
        var path = LocateDotNetEfToolExecutable();

        if (path != null) return path;

        var exeName = GetDotNetEfToolExeName();
        throw new Exception($"Could not find the '{exeName}' executable. " +
                            $"Verify that '{exeName}' is in your PATH, or ensure the that you have it installed in " +
                            "the dotnet global tools path under '{UserHomeDirectory}/.dotnet/tools'.");
    }

    public static string? LocateDotNetEfToolExecutable()
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

    public static Version? GetDotNetEfToolVersion(string dotNetEfToolExePath)
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
}
