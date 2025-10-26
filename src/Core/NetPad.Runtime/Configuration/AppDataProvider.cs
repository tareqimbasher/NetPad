using System.IO;
using NetPad.Application;
using NetPad.IO;

namespace NetPad.Configuration;

/// <summary>
/// Provides paths to common directories where the application stores data and files.
/// </summary>
public static class AppDataProvider
{
    /// <summary>
    /// Path of NetPad's local data directory.
    /// </summary>
    public static readonly DirectoryPath AppDataDirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppIdentifier.AppName);

    public static readonly DirectoryPath LogDirectoryPath = AppDataDirectoryPath.Combine("Logs");
    public static readonly DirectoryPath TempDirectoryPath = Path.Combine(Path.GetTempPath(), AppIdentifier.AppName);
    public static readonly DirectoryPath ExternalProcessesDirectoryPath = TempDirectoryPath.Combine("Processes/External");
    public static readonly DirectoryPath ClientServerProcessesDirectoryPath = TempDirectoryPath.Combine("Processes/ClientServer");
    public static readonly DirectoryPath TypedDataContextTempDirectoryPath = TempDirectoryPath.Combine("TypedDataContexts");
    public static readonly DirectoryPath TypedDataContextCacheDirectoryPath = AppDataDirectoryPath.Combine("Cache", "TypedDataContexts");

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

        public static DirectoryPath AutoSaveScriptsDirectoryPath { get; } = AppDataDirectoryPath.Combine("AutoSave", "Scripts");

        public static DirectoryPath PackageCacheDirectoryPath { get; } = AppDataDirectoryPath.Combine("Cache", "Packages");
    }
}
