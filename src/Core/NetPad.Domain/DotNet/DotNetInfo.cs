using System;
using System.Diagnostics;
using System.IO;
using NetPad.Utilities;

namespace NetPad.DotNet;

public static class DotNetInfo
{
    private static readonly object _dotNetExeLocateLock = new object();
    private static readonly object _dotNetEfToolExeLocateLock = new object();
    private static string? _dotNetPath;
    private static string? _dotNetEfToolPath;

    public static string LocateDotNetRootDirectoryOrThrow()
    {
        return LocateDotNetRootDirectory() ?? throw new Exception("Could not find the dotnet ROOT directory.");
    }

    public static string? LocateDotNetRootDirectory()
    {
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT")
                         ?? Environment.GetEnvironmentVariable("DOTNET_INSTALL_DIR")
                         ?? (PlatformUtils.IsWindowsPlatform() ? @"C:\Program Files\dotnet" : "/usr/local/share/dotnet");

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
            string? path;

            if (_dotNetPath != null)
            {
                return _dotNetPath;
            }

            var exeName = GetDotNetExeName();

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
            string? path;

            if (_dotNetEfToolPath != null)
            {
                return _dotNetEfToolPath;
            }

            var exeName = GetDotNetEfToolExeName();

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

    private static string GetDotNetExeName()
    {
        return PlatformUtils.IsWindowsPlatform() ? "dotnet.exe" : "dotnet";
    }

    private static string GetDotNetEfToolExeName()
    {
        return PlatformUtils.IsWindowsPlatform() ? "dotnet-ef.exe" : "dotnet-ef";
    }
}
