using System;
using System.IO;

namespace NetPad
{
    public class Settings
    {
        public Settings()
        {
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

        public Theme Theme { get; private set; }
        public string ScriptsDirectoryPath { get; private set; }
        public string PackageCacheDirectoryPath { get; private set; }

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
