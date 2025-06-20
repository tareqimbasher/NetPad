using System.IO;
using System.Text.Json.Serialization;

namespace NetPad.Configuration;

/// <summary>
/// Application-wide settings.
/// </summary>
public class Settings : ISettingsOptions
{
    private const string LatestSettingsVersion = "1.0";

    public Settings()
    {
        DefaultMissingValues();
    }

    public Settings(string scriptsDirectoryPath, string packageCacheDirectoryPath) : this()
    {
        ScriptsDirectoryPath = scriptsDirectoryPath;
        PackageCacheDirectoryPath = packageCacheDirectoryPath;
    }

    [JsonInclude] public Version Version { get; private set; } = null!;
    [JsonInclude] public bool? AutoCheckUpdates { get; private set; }
    [JsonInclude] public string? DotNetSdkDirectoryPath { get; private set; }
    [JsonInclude] public string ScriptsDirectoryPath { get; private set; } = null!;
    [JsonInclude] public string AutoSaveScriptsDirectoryPath { get; private set; } = null!;
    [JsonInclude] public string PackageCacheDirectoryPath { get; private set; } = null!;
    [JsonInclude] public AppearanceOptions Appearance { get; private set; } = null!;
    [JsonInclude] public EditorOptions Editor { get; private set; } = null!;
    [JsonInclude] public ResultsOptions Results { get; private set; } = null!;
    [JsonInclude] public StyleOptions Styles { get; private set; } = null!;
    [JsonInclude] public KeyboardShortcutOptions KeyboardShortcuts { get; private set; } = null!;
    [JsonInclude] public OmniSharpOptions OmniSharp { get; set; } = null!;

    public Settings SetAutoCheckUpdates(bool autoCheckUpdates)
    {
        AutoCheckUpdates = autoCheckUpdates;
        return this;
    }

    public Settings SetDotNetSdkDirectoryPath(string? dotNetSdkDirectoryPath)
    {
        DotNetSdkDirectoryPath = dotNetSdkDirectoryPath;
        return this;
    }

    public Settings SetScriptsDirectoryPath(string scriptsDirectoryPath)
    {
        ScriptsDirectoryPath =
            scriptsDirectoryPath ?? throw new ArgumentNullException(nameof(scriptsDirectoryPath));
        return this;
    }

    public Settings SetPackageCacheDirectoryPath(string packageCacheDirectoryPath)
    {
        PackageCacheDirectoryPath = packageCacheDirectoryPath ??
                                    throw new ArgumentNullException(nameof(packageCacheDirectoryPath));
        return this;
    }

    public Settings SetAppearanceOptions(AppearanceOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        Appearance
            .SetTheme(options.Theme)
            .SetIconTheme(options.IconTheme)
            .SetShowScriptRunStatusIndicatorInTab(options.ShowScriptRunStatusIndicatorInTab)
            .SetShowScriptRunStatusIndicatorInScriptsList(options.ShowScriptRunStatusIndicatorInScriptsList)
            .SetShowScriptRunningIndicatorInScriptsList(options.ShowScriptRunningIndicatorInScriptsList)
            .SetTitlebarOptions(options.Titlebar);

        return this;
    }

    public Settings SetEditorOptions(EditorOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        Editor.SetMonacoOptions(options.MonacoOptions);

        return this;
    }

    public Settings SetResultsOptions(ResultsOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        Results
            .SetOpenOnRun(options.OpenOnRun)
            .SetTextWrap(options.TextWrap)
            .SetFont(options.Font)
            .SetMaxSerializationDepth(options.MaxSerializationDepth)
            .SetMaxCollectionSerializeLengthDepth(options.MaxCollectionSerializeLength);

        return this;
    }

    public Settings SetStyleOptions(StyleOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        Styles
            .SetEnabled(options.Enabled)
            .SetCustomCss(options.CustomCss);

        return this;
    }

    public Settings SetKeyboardShortcutOptions(KeyboardShortcutOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        KeyboardShortcuts
            .SetShortcuts(options.Shortcuts);

        return this;
    }

    public Settings SetOmniSharpOptions(OmniSharpOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        OmniSharp
            .SetEnabled(options.Enabled)
            .SetExecutablePath(options.ExecutablePath)
            .SetEnableAnalyzersSupport(options.EnableAnalyzersSupport)
            .SetEnableImportCompletion(options.EnableImportCompletion)
            .SetEnableSemanticHighlighting(options.EnableSemanticHighlighting)
            .SetEnableCodeLensReferences(options.EnableCodeLensReferences)
            .SetDiagnosticsOptions(options.Diagnostics)
            .SetInlayHintsOptions(options.InlayHints);

        return this;
    }


    public void DefaultMissingValues()
    {
        // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        // ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract

        if (Version == null)
            Version = Version.Parse(LatestSettingsVersion);

        AutoCheckUpdates ??= true;

        if (string.IsNullOrWhiteSpace(ScriptsDirectoryPath))
            ScriptsDirectoryPath = AppDataProvider.Defaults.ScriptsDirectoryPath.Path;

        if (string.IsNullOrWhiteSpace(AutoSaveScriptsDirectoryPath))
            AutoSaveScriptsDirectoryPath = AppDataProvider.Defaults.AutoSaveScriptsDirectoryPath.Path;

        if (string.IsNullOrWhiteSpace(PackageCacheDirectoryPath))
            PackageCacheDirectoryPath = AppDataProvider.Defaults.PackageCacheDirectoryPath.Path;

        (Appearance ??= new()).DefaultMissingValues();
        (Editor ??= new()).DefaultMissingValues();
        (Results ??= new()).DefaultMissingValues();
        (Styles ??= new()).DefaultMissingValues();
        (KeyboardShortcuts ??= new()).DefaultMissingValues();
        (OmniSharp ??= new()).DefaultMissingValues();

        // ReSharper enable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        // ReSharper enable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
    }

    /// <summary>
    /// Upgrades a <see cref="Settings"/> object to the latest version.
    /// </summary>
    /// <returns>Returns <see langword="true"/> if changes were made, otherwise <see langword="false"/>.</returns>
    public bool Upgrade()
    {
        bool changesMade = false;

        var scriptsDirWritable = Try.Run(() =>
        {
            if (!Directory.Exists(ScriptsDirectoryPath))
            {
                Directory.CreateDirectory(ScriptsDirectoryPath);
                return true;
            }

            return FileSystemUtil.IsDirectoryWritable(ScriptsDirectoryPath);
        });

        if (!scriptsDirWritable && ScriptsDirectoryPath != AppDataProvider.Defaults.FallbackScriptsDirectoryPath.Path)
        {
            ScriptsDirectoryPath = AppDataProvider.Defaults.FallbackScriptsDirectoryPath.Path;
            changesMade = true;
        }

        return changesMade;
    }
}
