using System;
using System.IO;

namespace NetPad.Configuration
{
    public class Settings
    {
        public static readonly string AppDataFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NetPad");

        public static readonly string LogFolderPath = Path.Combine(AppDataFolderPath, "Logs");

        public Settings()
        {
            // Defaults
            Theme = Theme.Dark;

            ScriptsDirectoryPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Documents",
                "NetPad",
                "Scripts");

            AutoSaveScriptsDirectoryPath = Path.Combine(AppDataFolderPath,
                "AutoSave",
                "Scripts");

            PackageCacheDirectoryPath = Path.Combine(AppDataFolderPath,
                "Cache",
                "Packages");

            EditorOptions = new EditorOptions
            {
                CodeCompletion = new CodeCompletionOptions
                {
                    Enabled = true,
                    Provider = new OmniSharpCodeCompletionProviderOptions()
                },
                MonacoOptions = new
                {
                    cursorBlinking = "blink",
                    lineNumbers = "on",
                    wordWrap = "off",
                    mouseWheelZoom = true,
                    minimap = new
                    {
                        enabled = true
                    }
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
        public string AutoSaveScriptsDirectoryPath { get; set; }
        public string PackageCacheDirectoryPath { get; set; }
        public string? EditorBackgroundColor { get; set; }
        public EditorOptions EditorOptions { get; set; }
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

        public Settings SetEditorOptions(EditorOptions options)
        {
            EditorOptions = options ?? throw new ArgumentNullException(nameof(options));
            return this;
        }

        public Settings SetResultsOptions(ResultsOptions resultsOptions)
        {
            ResultsOptions = resultsOptions ?? throw new ArgumentNullException(nameof(resultsOptions));
            return this;
        }
    }
}
