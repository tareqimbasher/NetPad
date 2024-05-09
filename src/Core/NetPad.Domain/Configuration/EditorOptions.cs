using System.Text.Json.Serialization;

namespace NetPad.Configuration;

public class EditorOptions : ISettingsOptions
{
    public EditorOptions()
    {
        DefaultMissingValues();
    }

    [JsonInclude] public string? BackgroundColor { get; private set; }
    [JsonInclude] public object MonacoOptions { get; private set; } = null!;

    public EditorOptions SetBackgroundColor(string? backgroundColor)
    {
        BackgroundColor = backgroundColor;
        return this;
    }

    public EditorOptions SetMonacoOptions(object monacoOptions)
    {
        MonacoOptions = monacoOptions;
        return this;
    }

    public void DefaultMissingValues()
    {
        // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

        if (MonacoOptions == null)
        {
            MonacoOptions = new
            {
                cursorBlinking = "smooth",
                lineNumbers = "on",
                wordWrap = "off",
                mouseWheelZoom = true,
                minimap = new
                {
                    enabled = true
                }
            };
        }

        // ReSharper enable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
    }
}
