using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using NetPad.Common;

namespace NetPad.Configuration;

public class AppearanceOptions : ISettingsOptions
{
    /// <summary>
    /// The theme family used when none is set, or when the configured one is not installed.
    /// </summary>
    public const string DefaultThemeFamily = "inkwell";

    public AppearanceOptions()
    {
        ThemeFamily = DefaultThemeFamily;
        Mode = ThemeMode.System;
        Background = ThemeBackground.Palette;
        ShowScriptRunStatusIndicatorInTab = true;
        ScriptRunStatusIndicatorInExplorer = StatusIndicatorVisibility.Always;
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

    /// <summary>
    /// Whether the backgrounds follow the palette or stay neutral under every palette.
    /// </summary>
    [JsonInclude]
    [JsonConverter(typeof(TolerantJsonStringEnumConverter<ThemeBackground>))]
    public ThemeBackground Background { get; private set; }

    [JsonInclude] public bool ShowScriptRunStatusIndicatorInTab { get; private set; }

    [JsonInclude]
    [JsonConverter(typeof(TolerantJsonStringEnumConverter<StatusIndicatorVisibility>))]
    public StatusIndicatorVisibility ScriptRunStatusIndicatorInExplorer { get; private set; }
    [JsonInclude] public TitlebarOptions Titlebar { get; private set; } = null!;

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

    public AppearanceOptions SetBackground(ThemeBackground background)
    {
        Background = background;
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

        (Titlebar ??= new TitlebarOptions()).DefaultMissingValues();
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
