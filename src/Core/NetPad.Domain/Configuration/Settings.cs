using System;
using System.IO;

namespace NetPad.Configuration
{
    public class Settings
    {
        public Settings()
        {
            // Defaults
            Theme = Theme.Dark;
            ScriptsDirectoryPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Documents",
                "NetPad");
            PackageCacheDirectoryPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NetPad",
                "Cache",
                "Packages");
        }

        public Settings(Theme theme, string scriptsDirectoryPath, string packageCacheDirectoryPath) : this()
        {
            Theme = theme;
            ScriptsDirectoryPath = scriptsDirectoryPath;
            PackageCacheDirectoryPath = packageCacheDirectoryPath;
        }

        public Theme Theme { get; set; }
        public string ScriptsDirectoryPath { get; set; }
        public string PackageCacheDirectoryPath { get; set; }

        public Settings SetTheme(Theme theme)
        {
            Theme = theme;
            return this;
        }

        public Settings SetScriptsDirectoryPath(string scriptsDirectoryPath)
        {
            ScriptsDirectoryPath = scriptsDirectoryPath ?? throw new ArgumentNullException(nameof(scriptsDirectoryPath));
            return this;
        }

        public Settings SetPackageCacheDirectoryPath(string packageCacheDirectoryPath)
        {
            PackageCacheDirectoryPath = packageCacheDirectoryPath ?? throw new ArgumentNullException(nameof(packageCacheDirectoryPath));
            return this;
        }

        public void Save()
        {
            throw new NotImplementedException();
        }
    }

    public enum Theme
    {
        Light, Dark
    }
}
