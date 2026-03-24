using System.Runtime.InteropServices;

namespace NetPad.Utilities;

public static class PlatformUtil
{
    private static readonly IReadOnlyList<Architecture> _supportedArchitectures =
    [
        Architecture.X64,
        Architecture.X86,
        Architecture.Arm64
    ];

    public static OSPlatform GetOSPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return OSPlatform.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return OSPlatform.OSX;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return OSPlatform.Linux;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            return OSPlatform.FreeBSD;

        throw new Exception($"Could not determine OS platform. OS: {RuntimeInformation.OSDescription}");
    }

    public static bool IsOSWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsOSMacOs() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public static bool IsOSLinuxOrFreeBsd() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                                               RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);

    public static bool IsOsArchitectureSupported(bool throwIfNotSupported = false)
    {
        bool supported = _supportedArchitectures.Contains(RuntimeInformation.OSArchitecture);

        if (!supported && throwIfNotSupported)
        {
            throw new PlatformNotSupportedException(
                $"OS Architecture '{RuntimeInformation.OSArchitecture}' is not supported. OS: ({RuntimeInformation.OSDescription})");
        }

        return supported;
    }

    public static string GetPlatformExecutableExtension() => IsOSWindows() ? ".exe" : string.Empty;

    public static char PathSeparator => IsOSWindows() ? ';' : ':';

    /// <summary>
    /// Appends a directory to an existing PATH value using the platform-specific separator.
    /// </summary>
    public static string AppendToPathVariable(string? existingPath, string directory)
    {
        if (string.IsNullOrWhiteSpace(existingPath))
            return directory;

        return existingPath.EndsWith(PathSeparator)
            ? $"{existingPath}{directory}"
            : $"{existingPath}{PathSeparator}{directory}";
    }
}
