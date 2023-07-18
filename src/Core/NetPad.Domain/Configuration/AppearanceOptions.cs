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

    public void DefaultMissingValues()
    {
    }
}
