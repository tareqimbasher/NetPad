using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using NetPad.Configuration;

namespace NetPad.DotNet;

public class DotNetInfo(Settings settings, ILogger<DotNetInfo> logger) : IDotNetInfo
{
    private readonly DotNetEnvironment _environment = new();
    private readonly DotNetCliRunner _dotNetCliRunner = new();
    private readonly DotNetPathResolver _dotNetPathResolver = new();

    private readonly object _pathReportLock = new();
    private volatile DotNetPathReport? _pathReport;

    private readonly object _dotNetExeLocateLock = new();
    private volatile string? _dotNetExecutablePath;

    private readonly object _dotNetEfToolExeLocateLock = new();
    private volatile string? _dotNetEfToolPath;

    private static readonly object _dotNetRuntimeVersionsLocateLock = new();
    private static volatile DotNetRuntimeVersion[]? _dotNetRuntimeVersions;

    private static readonly string _dotNetExeName = PlatformUtil.IsOSWindows() ? "dotnet.exe" : "dotnet";
    private static readonly object _dotNetSdkVersionsLocateLock = new();
    private static volatile DotNetSdkVersion[]? _dotNetSdkVersions;

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
        return GetPathReport().ResolvedPath?.Path;
    }

    private DotNetPathReport GetPathReport()
    {
        var report = _pathReport;
        if (report != null)
        {
            return report;
        }

        lock (_pathReportLock)
        {
            report = _pathReport;
            if (report != null)
            {
                return report;
            }

            report = _dotNetPathResolver.FindDotNetInstallDir(settings);

            if (report.ResolvedPath is not null)
            {
                Environment.SetEnvironmentVariable("DOTNET_ROOT", report.ResolvedPath.Path);
                logger.LogDebug("Resolved primary .NET installation: {ResolvedPath} ({ValidCount} valid installation(s) found)",
                    report.ResolvedPath.Path, report.AllValidPaths.Count);
            }
            else
            {
                logger.LogWarning("No valid .NET installation found across {SearchCount} searched location(s)",
                    report.SearchSteps.Count);
            }

            _pathReport = report;
        }

        return report;
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
            logger.LogDebug("Resolved dotnet executable: {ExePath}", exePath ?? "(not found)");
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

            var allRuntimes = new List<DotNetRuntimeVersion>();

            foreach (var installDir in GetPathReport().AllValidPaths)
            {
                var exePath = Path.Combine(installDir.Path, _dotNetExeName);
                if (!File.Exists(exePath)) continue;

                try
                {
                    var output = _dotNetCliRunner.ExecuteCommand(exePath, "--list-runtimes");
                    logger.LogDebug("dotnet --list-runtimes output from {ExePath}:\n{Output}", exePath, output);
                    allRuntimes.AddRange(ParseRuntimeVersions(output, installDir.Path));
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to query runtimes from {InstallDir}", installDir.Path);
                }
            }

            _dotNetRuntimeVersions = allRuntimes
                .GroupBy(r => (r.FrameworkName, r.Version))
                .Select(g => g.First())
                .OrderBy(r => r.FrameworkName)
                .ThenBy(r => r.Version)
                .ToArray();

            logger.LogDebug("Found {Count} unique .NET runtime(s) across all installations", _dotNetRuntimeVersions.Length);
        }

        return _dotNetRuntimeVersions;
    }

    private static DotNetRuntimeVersion[] ParseRuntimeVersions(string output, string fallbackDotNetRootDir)
    {
        // Output format: {name} {version} [{runtimePath}]
        // Example: Microsoft.NETCore.App 9.0.3 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
        // The root is found by walking up from the bracket path past "shared/{FrameworkName}".
        return output.Split(Environment.NewLine)
            .Select(line =>
            {
                var trimmed = line.Trim();
                var parts = trimmed.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) return null;

                var name = parts[0].Trim();
                var versionStr = parts[1].Trim();
                if (string.IsNullOrEmpty(name) || !SemanticVersion.TryParse(versionStr, out var version))
                    return null;

                var rootDir = ExtractDotNetRootFromBracketPath(trimmed) ?? fallbackDotNetRootDir;
                return new DotNetRuntimeVersion(name, version, rootDir);
            })
            .Where(v => v != null)
            .ToArray()!;
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

            var allSdks = new List<DotNetSdkVersion>();

            foreach (var installDir in GetPathReport().AllValidPaths)
            {
                var exePath = Path.Combine(installDir.Path, _dotNetExeName);
                if (!File.Exists(exePath)) continue;

                try
                {
                    var output = _dotNetCliRunner.ExecuteCommand(exePath, "--list-sdks");
                    logger.LogDebug("dotnet --list-sdks output from {ExePath}:\n{Output}", exePath, output);
                    allSdks.AddRange(ParseSdkVersions(output, installDir.Path));
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to query SDKs from {InstallDir}", installDir.Path);
                }
            }

            _dotNetSdkVersions = allSdks
                .GroupBy(s => s.Version)
                .Select(g => g.First())
                .OrderBy(s => s.Version)
                .ToArray();

            logger.LogDebug("Found {Count} unique .NET SDK(s) across all installations", _dotNetSdkVersions.Length);
        }

        return _dotNetSdkVersions;
    }

    private static DotNetSdkVersion[] ParseSdkVersions(string output, string fallbackDotNetRootDir)
    {
        // Output format: {version} [{sdkParentPath}]
        // Example: 9.0.200 [C:\Program Files\dotnet\sdk]
        // The root is the parent of the bracket path (removing the trailing "sdk" segment).
        return output.Split(Environment.NewLine)
            .Select(line =>
            {
                var trimmed = line.Trim();
                var versionStr = trimmed.Split(' ', 2)[0].Trim();
                if (string.IsNullOrWhiteSpace(versionStr) ||
                    !SemanticVersion.TryParse(versionStr, out var version))
                    return null;

                var rootDir = ExtractDotNetRootFromBracketPath(trimmed) ?? fallbackDotNetRootDir;
                return new DotNetSdkVersion(version, rootDir);
            })
            .Where(x => x is not null)
            .ToArray()!;
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


    public string? LocateDotNetRootDirectoryForFramework(DotNetFrameworkVersion frameworkVersion)
    {
        var matchingSdk = GetDotNetSdkVersions()
            .Where(s => s.IsSupported()
                        && s.GetFrameworkVersion() == frameworkVersion
                        && s.DotNetRootDirectory != null)
            .MaxBy(s => s.Version);

        var rootDir = matchingSdk?.DotNetRootDirectory;
        logger.LogDebug("Resolved .NET root for {Framework}: {RootDir} (SDK {Version})",
            frameworkVersion, rootDir ?? "(none)", matchingSdk?.Version.ToString() ?? "N/A");
        return rootDir;
    }

    public string? LocateDotNetExecutableForFramework(DotNetFrameworkVersion frameworkVersion)
    {
        var rootDir = LocateDotNetRootDirectoryForFramework(frameworkVersion);

        if (rootDir != null && DotNetPathResolver.IsValidDotNetSdkRootDirectory(rootDir))
        {
            return Path.Combine(rootDir, _dotNetExeName);
        }

        return null;
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
                    UseShellExecute = false,
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
        var output = _dotNetCliRunner.ExecuteCommand(dotNetEfToolExePath, "--version");
        return SemanticVersion.TryParse(output.Split(Environment.NewLine).Skip(1).FirstOrDefault(), out var version)
            ? version
            : null;
    }

    /// <summary>
    /// Extracts the .NET root directory from the bracket path in dotnet CLI output.
    /// Both --list-sdks and --list-runtimes include a path in square brackets that indicates
    /// where the SDK/runtime lives. The .NET root is the ancestor directory above the
    /// well-known subdirectory segments (sdk, shared, etc.).
    /// </summary>
    /// <example>
    /// "9.0.200 [C:\Program Files\dotnet\sdk]" → "C:\Program Files\dotnet"
    /// "Microsoft.NETCore.App 9.0.3 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]" → "C:\Program Files\dotnet"
    /// </example>
    private static string? ExtractDotNetRootFromBracketPath(string line)
    {
        var openBracket = line.LastIndexOf('[');
        var closeBracket = line.LastIndexOf(']');
        if (openBracket < 0 || closeBracket <= openBracket) return null;

        var bracketPath = line[(openBracket + 1)..closeBracket].Trim();
        if (string.IsNullOrEmpty(bracketPath)) return null;

        // Walk up until we find a directory that contains the dotnet executable
        var dir = new DirectoryInfo(bracketPath);
        while (dir != null)
        {
            if (DotNetPathResolver.IsValidDotNetSdkRootDirectory(dir.FullName))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    private static string GetDotNetEfToolExeName()
    {
        return PlatformUtil.IsOSWindows() ? "dotnet-ef.exe" : "dotnet-ef";
    }
}
