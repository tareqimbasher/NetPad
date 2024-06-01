using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace NetPad.Configuration;

public class AppearanceOptions : ISettingsOptions
{
    public AppearanceOptions()
    {
        Theme = Theme.Dark;
        IconTheme = IconTheme.Default;
        ShowScriptRunStatusIndicatorInTab = true;
        ShowScriptRunStatusIndicatorInScriptsList = false;
        ShowScriptRunningIndicatorInScriptsList = false;
        DefaultMissingValues();
    }

    [JsonInclude] public Theme Theme { get; private set; }
    [JsonInclude] public IconTheme IconTheme { get; private set; }
    [JsonInclude] public bool ShowScriptRunStatusIndicatorInTab { get; private set; }
    [JsonInclude] public bool ShowScriptRunStatusIndicatorInScriptsList { get; private set; }
    [JsonInclude] public bool ShowScriptRunningIndicatorInScriptsList { get; private set; }
    [JsonInclude] public TitlebarOptions Titlebar { get; private set; } = null!;

    public AppearanceOptions SetTheme(Theme theme)
    {
        Theme = theme;
        return this;
    }

    public AppearanceOptions SetIconTheme(IconTheme iconTheme)
    {
        IconTheme = iconTheme;
        return this;
    }

    public AppearanceOptions SetShowScriptRunStatusIndicatorInScriptsList(bool showScriptRunStatusIndicatorInScriptsList)
    {
        ShowScriptRunStatusIndicatorInScriptsList = showScriptRunStatusIndicatorInScriptsList;
        return this;
    }

    public AppearanceOptions SetShowScriptRunStatusIndicatorInTab(bool showScriptRunStatusIndicatorInTab)
    {
        ShowScriptRunStatusIndicatorInTab = showScriptRunStatusIndicatorInTab;
        return this;
    }

    public AppearanceOptions SetShowScriptRunningIndicatorInScriptsList(bool showScriptRunningIndicatorInScriptsList)
    {
        ShowScriptRunningIndicatorInScriptsList = showScriptRunningIndicatorInScriptsList;
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
        (Titlebar ??= new TitlebarOptions()).DefaultMissingValues();
    }
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
