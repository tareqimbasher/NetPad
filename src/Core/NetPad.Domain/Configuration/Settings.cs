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
                "NetPad",
                "Scripts");

            PackageCacheDirectoryPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NetPad",
                "Cache",
                "Packages");

            EditorOptions = new
            {
                cursorBlinking = "blink",
                lineNumbers = "on",
                wordWrap = "off",
                mouseWheelZoom = true,
                minimap = new
                {
                    enabled = true
                }
            };

            ResultsOptions = new ResultsOptions
            {
                OpenOnRun = true,
                TextWrap = false
            };
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
        public string? EditorBackgroundColor { get; set; }
        public object EditorOptions { get; set; }
        public ResultsOptions ResultsOptions { get; set; }

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

        public Settings SetEditorBackgroundColor(string? editorBackgroundColor)
        {
            EditorBackgroundColor = editorBackgroundColor;
            return this;
        }

        public Settings SetEditorOptions(object options)
        {
            EditorOptions = options ?? new object();
            return this;
        }

        public Settings SetResultsOptions(ResultsOptions resultsOptions)
        {
            ResultsOptions = resultsOptions ?? throw new ArgumentNullException(nameof(resultsOptions));
            return this;
        }
    }
}
