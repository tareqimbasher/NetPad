using System.Text.Json.Serialization;

namespace NetPad.Configuration;

public class StyleOptions : ISettingsOptions
{
    public StyleOptions()
    {
        DefaultMissingValues();
    }

    [JsonInclude] public bool Enabled { get; private set; }
    [JsonInclude] public string? CustomCss { get; private set; }

    public StyleOptions SetEnabled(bool enabled)
    {
        Enabled = enabled;
        return this;
    }

    public StyleOptions SetCustomCss(string? customCss)
    {
        CustomCss = customCss;
        return this;
    }

    public void DefaultMissingValues()
    {
    }
}
