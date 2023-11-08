using System;
using System.Text.Json.Serialization;

namespace NetPad.Configuration;

public class Settings : ISettingsOptions
{
    public const string LatestSettingsVersion = "1.0";

    public Settings()
    {
        DefaultMissingValues();
    }

    public Settings(string scriptsDirectoryPath, string packageCacheDirectoryPath) : this()
    {
        ScriptsDirectoryPath = scriptsDirectoryPath;
        PackageCacheDirectoryPath = packageCacheDirectoryPath;
    }

    [JsonInclude] public Version Version { get; private set; }
    [JsonInclude] public bool? AutoCheckUpdates { get; private set; }
    [JsonInclude] public string? DotNetSdkDirectoryPath { get; private set; }
    [JsonInclude] public string ScriptsDirectoryPath { get; private set; }
    [JsonInclude] public string AutoSaveScriptsDirectoryPath { get; private set; }
    [JsonInclude] public string PackageCacheDirectoryPath { get; private set; }
    [JsonInclude] public AppearanceOptions Appearance { get; private set; }
    [JsonInclude] public EditorOptions Editor { get; private set; }
    [JsonInclude] public ResultsOptions Results { get; private set; }
    [JsonInclude] public KeyboardShortcutOptions KeyboardShortcuts { get; private set; }
    [JsonInclude] public OmniSharpOptions OmniSharp { get; set; }

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

        Editor
            .SetBackgroundColor(options.BackgroundColor)
            .SetMonacoOptions(options.MonacoOptions);

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
        (KeyboardShortcuts ??= new()).DefaultMissingValues();
        (OmniSharp ??= new()).DefaultMissingValues();

        // ReSharper enable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
    }

    /// <summary>
    /// Upgrades a <see cref="Settings"/> object to the latest version.
    /// </summary>
    /// <returns>True if changes were made, otherwise false.</returns>
    public bool Upgrade()
    {
        return false;
    }
}
