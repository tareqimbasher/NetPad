using System;
using System.IO;
using NetPad.Application;
using NetPad.IO;

namespace NetPad.Configuration;

public static class AppDataProvider
{
    /// <summary>
    /// Path of NetPad's local data directory.
    /// </summary>
    public static readonly DirectoryPath AppDataDirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppIdentifier.AppName);

    /// <summary>
    /// Path of where the app stores logs.
    /// </summary>
    public static readonly DirectoryPath LogDirectoryPath = Path.Combine(AppDataDirectoryPath.Path, "Logs");

    public static readonly DirectoryPath TempDirectoryPath = Path.Combine(Path.GetTempPath(), AppIdentifier.AppName);
    public static readonly DirectoryPath ExternalProcessesDirectoryPath = TempDirectoryPath.Combine("Processes");
    public static readonly DirectoryPath TypedDataContextTempDirectoryPath = TempDirectoryPath.Combine("TypedDataContexts");
    public static readonly DirectoryPath TypedDataContextCacheDirectoryPath = AppDataDirectoryPath.Combine("Cache", "TypedDataContexts");

    public static class Defaults
    {
        public static DirectoryPath ScriptsDirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Documents",
            AppIdentifier.AppName,
            "Scripts");

        public static DirectoryPath AutoSaveScriptsDirectoryPath = AppDataDirectoryPath.Combine("AutoSave", "Scripts");

        public static DirectoryPath PackageCacheDirectoryPath = AppDataDirectoryPath.Combine("Cache", "Packages");
    }
}
