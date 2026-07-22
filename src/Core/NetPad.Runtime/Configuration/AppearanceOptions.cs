using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using NetPad.Common;
using NJsonSchema.Annotations;

namespace NetPad.Configuration;

public class AppearanceOptions : ISettingsOptions
{
    /// <summary>
    /// The theme family used when none is set, or when the configured one is not installed.
    /// </summary>
    public const string DefaultThemeFamily = "netpad";

    private string? _retiredTheme;
    private bool? _retiredShowStatusIndicatorInScriptsList;
    private bool? _retiredShowRunningIndicatorInScriptsList;

    public AppearanceOptions()
    {
        ThemeFamily = DefaultThemeFamily;
        Mode = ThemeMode.System;
        ShowScriptRunStatusIndicatorInTab = true;
        ScriptRunStatusIndicatorInExplorer = StatusIndicatorVisibility.Off;
        DefaultMissingValues();
    }

    /// <summary>
    /// The palette the app paints with. An unknown family falls back to <see cref="DefaultThemeFamily"/>.
    /// </summary>
    [JsonInclude] public string ThemeFamily { get; private set; }

    // The converters are declared here rather than on the enums: a converter registered on the
    // serializer's options outranks one attached to a type, and the shared serializer registers
    // JsonStringEnumConverter. A property-level converter outranks both.
    [JsonInclude]
    [JsonConverter(typeof(TolerantJsonStringEnumConverter<ThemeMode>))]
    public ThemeMode Mode { get; private set; }

    [JsonInclude] public bool ShowScriptRunStatusIndicatorInTab { get; private set; }

    [JsonInclude]
    [JsonConverter(typeof(TolerantJsonStringEnumConverter<StatusIndicatorVisibility>))]
    public StatusIndicatorVisibility ScriptRunStatusIndicatorInExplorer { get; private set; }
    [JsonInclude] public TitlebarOptions Titlebar { get; private set; } = null!;

    /// <summary>
    /// Retired in favor of <see cref="ThemeFamily"/> + <see cref="Mode"/>. Settings files written
    /// before the split still carry it; the getter is null so it is never written back.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSchemaIgnore]
    public string? Theme
    {
        get => null;
        set => _retiredTheme = value;
    }

    /// <summary>
    /// Retired in favor of <see cref="ScriptRunStatusIndicatorInExplorer"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSchemaIgnore]
    public bool? ShowScriptRunStatusIndicatorInScriptsList
    {
        get => null;
        set => _retiredShowStatusIndicatorInScriptsList = value;
    }

    /// <summary>
    /// Retired in favor of <see cref="ScriptRunStatusIndicatorInExplorer"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSchemaIgnore]
    public bool? ShowScriptRunningIndicatorInScriptsList
    {
        get => null;
        set => _retiredShowRunningIndicatorInScriptsList = value;
    }

    public AppearanceOptions SetThemeFamily(string themeFamily)
    {
        ThemeFamily = string.IsNullOrWhiteSpace(themeFamily) ? DefaultThemeFamily : themeFamily;
        return this;
    }

    public AppearanceOptions SetMode(ThemeMode mode)
    {
        Mode = mode;
        return this;
    }

    public AppearanceOptions SetScriptRunStatusIndicatorInExplorer(StatusIndicatorVisibility visibility)
    {
        ScriptRunStatusIndicatorInExplorer = visibility;
        return this;
    }

    public AppearanceOptions SetShowScriptRunStatusIndicatorInTab(bool showScriptRunStatusIndicatorInTab)
    {
        ShowScriptRunStatusIndicatorInTab = showScriptRunStatusIndicatorInTab;
        return this;
    }

    public AppearanceOptions SetTitlebarOptions(TitlebarOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        Titlebar
            .SetType(options.Type)
            .SetWindowControlsPosition(options.WindowControlsPosition)
            .SetMainWindowVisibility(options.MainMenuVisibility);
        return this;
    }

    public void DefaultMissingValues()
    {
        if (string.IsNullOrWhiteSpace(ThemeFamily))
        {
            ThemeFamily = DefaultThemeFamily;
        }

        MigrateRetiredValues();

        (Titlebar ??= new TitlebarOptions()).DefaultMissingValues();
    }

    /// <summary>
    /// Folds values read from an older settings file onto the properties that replaced them. A
    /// retired value is consumed once: the new property already holds the user's choice afterwards.
    /// </summary>
    private void MigrateRetiredValues()
    {
        if (_retiredTheme != null)
        {
            // Before System mode existed the only choices were the two grounds, so an old file's
            // value is the mode the user picked.
            if (Enum.TryParse<ThemeMode>(_retiredTheme, true, out var mode) && mode != ThemeMode.System)
            {
                Mode = mode;
            }

            _retiredTheme = null;
        }

        if (_retiredShowStatusIndicatorInScriptsList != null || _retiredShowRunningIndicatorInScriptsList != null)
        {
            // The two booleans covered terminal statuses and the running state separately. Showing
            // terminal statuses is the broader of the two, so it maps to Always.
            ScriptRunStatusIndicatorInExplorer =
                _retiredShowStatusIndicatorInScriptsList == true ? StatusIndicatorVisibility.Always
                : _retiredShowRunningIndicatorInScriptsList == true ? StatusIndicatorVisibility.WhileRunning
                : StatusIndicatorVisibility.Off;

            _retiredShowStatusIndicatorInScriptsList = null;
            _retiredShowRunningIndicatorInScriptsList = null;
        }
    }
}

/// <summary>
/// When a surface marks a script with its run status.
/// </summary>
public enum StatusIndicatorVisibility
{
    Off,
    WhileRunning,
    Always
}

public enum TitlebarType
{
    Integrated,
    Native
}

public enum WindowControlsPosition
{
    Right,
    Left
}

public enum MainMenuVisibility
{
    AlwaysVisible,
    AutoHidden
}

public class TitlebarOptions : ISettingsOptions
{
    public TitlebarOptions()
    {
        // Default Native for OSX
        Type = PlatformUtil.GetOSPlatform() == OSPlatform.OSX ? TitlebarType.Native : TitlebarType.Integrated;
        WindowControlsPosition = WindowControlsPosition.Right;
        MainMenuVisibility = MainMenuVisibility.AlwaysVisible;
        DefaultMissingValues();
    }

    [JsonInclude] public TitlebarType Type { get; private set; }
    [JsonInclude] public WindowControlsPosition WindowControlsPosition { get; private set; }
    [JsonInclude] public MainMenuVisibility MainMenuVisibility { get; private set; }

    public TitlebarOptions SetType(TitlebarType type)
    {
        Type = type;
        return this;
    }

    public TitlebarOptions SetWindowControlsPosition(WindowControlsPosition position)
    {
        WindowControlsPosition = position;
        return this;
    }

    public TitlebarOptions SetMainWindowVisibility(MainMenuVisibility visibility)
    {
        MainMenuVisibility = visibility;
        return this;
    }

    public void DefaultMissingValues()
    {
    }
}
