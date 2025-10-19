using System.IO;
using NetPad.Application;
using NetPad.IO;

namespace NetPad.Configuration;

/// <summary>
/// Provides paths to common directories where the application stores data and files.
/// </summary>
public static class AppDataProvider
{
    private static readonly DirectoryPath _cacheDirectoryPath = GetCacheDirectoryPath();

    /// <summary>
    /// Path of NetPad's local data directory.
    /// </summary>
    public static readonly DirectoryPath AppDataDirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppIdentifier.AppName);

    /// <summary>Where log files are stored.</summary>
    public static readonly DirectoryPath LogDirectoryPath = AppDataDirectoryPath.Combine("Logs");

    /// <summary>A "NetPad" directory inside the system's default temp directory.</summary>
    public static readonly DirectoryPath TempDirectoryPath = Path.Combine(Path.GetTempPath(), AppIdentifier.AppName);

    /// <summary>A directory where the "External" execution model caches its script build/deployments.</summary>
    public static readonly DirectoryPath ExternalExecutionModelDeploymentCacheDirectoryPath =
        _cacheDirectoryPath.Combine("Execution", "External", "BuildCache");

    /// <summary>A directory where the "ClientServer" execution model deploys and runs its child script-host processes.</summary>
    public static readonly DirectoryPath ClientServerProcessesDirectoryPath =
        TempDirectoryPath.Combine("Execution", "ClientServer", "Processes");

    /// <summary>A directory where assets that are generated while scaffolding a database connection are stored temporarily.</summary>
    public static readonly DirectoryPath TypedDataContextTempDirectoryPath =
        TempDirectoryPath.Combine("TypedDataContexts");

    /// <summary>A directory where assets generated while scaffolding a database connection are cached.</summary>
    public static readonly DirectoryPath TypedDataContextCacheDirectoryPath =
        AppDataDirectoryPath.Combine("Cache", "TypedDataContexts");

    /// <summary>The path to the settings file.</summary>
    public static readonly FilePath SettingsFilePath = AppDataDirectoryPath.CombineFilePath("settings.json");

    /// <summary>
    /// All environment variables defined by this application.
    /// </summary>
    public static class AppEnvironmentVariables
    {
        public static string? CacheDirectory => Environment.GetEnvironmentVariable("NETPAD_CACHE_DIR");
    }

    private static DirectoryPath GetCacheDirectoryPath()
    {
        // Try to get it from env variable first
        var envVar = AppEnvironmentVariables.CacheDirectory;
        if (!string.IsNullOrWhiteSpace(envVar) && Path.IsPathRooted(envVar) &&
            DirectoryPath.TryParse(envVar, out var dirPath))
        {
            return dirPath;
        }

        if (PlatformUtil.IsOSLinuxOrFreeBsd())
        {
            //$XDG_CACHE_HOME if defined otherwise $HOME/.cache
            envVar = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            if (!string.IsNullOrWhiteSpace(envVar) && Path.IsPathRooted(envVar))
            {
                var path = Path.Combine(envVar, AppIdentifier.AppName);
                if (DirectoryPath.TryParse(path, out dirPath))
                {
                    return dirPath;
                }
            }

            envVar = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(envVar) && Path.IsPathRooted(envVar))
            {
                return Path.Combine(envVar, ".cache", AppIdentifier.AppName);
            }
        }
        else if (PlatformUtil.IsOSMacOs())
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Caches");
        }

        // If we got here we couldn't determine a proper cache directory
        return TempDirectoryPath.Combine("Cache");
    }

    public static class Defaults
    {
        private static DirectoryPath? _scriptsDirectoryPath;

        public static DirectoryPath ScriptsDirectoryPath
        {
            get
            {
                if (_scriptsDirectoryPath != null)
                {
                    return _scriptsDirectoryPath;
                }

                DirectoryPath dir;

                var documentsDir = new DirectoryPath(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Documents"));

                if (documentsDir.IsWritable())
                {
                    dir = documentsDir.Combine(AppIdentifier.AppName, "Scripts");
                }
                else
                {
                    dir = FallbackScriptsDirectoryPath;
                }

                _scriptsDirectoryPath = dir;

                return _scriptsDirectoryPath;
            }
        }

        public static DirectoryPath FallbackScriptsDirectoryPath { get; } = AppDataDirectoryPath.Combine("Scripts");

        public static DirectoryPath AutoSaveScriptsDirectoryPath { get; } =
            AppDataDirectoryPath.Combine("AutoSave", "Scripts");

        public static DirectoryPath PackageCacheDirectoryPath { get; } =
            AppDataDirectoryPath.Combine("Cache", "Packages");
    }
}
