using System.Text.Json.Serialization;

namespace NetPad.Configuration;

public class LocalizationOptions : ISettingsOptions
{
    public LocalizationOptions()
    {
        DefaultMissingValues();
    }

    /// <summary>
    /// Whether compiler messages, quick fixes and hints use English instead of the system language.
    /// </summary>
    [JsonInclude] public bool UseEnglishForCodeTooling { get; private set; }

    public LocalizationOptions SetUseEnglishForCodeTooling(bool useEnglishForCodeTooling)
    {
        UseEnglishForCodeTooling = useEnglishForCodeTooling;
        return this;
    }

    public void DefaultMissingValues()
    {
    }
}
