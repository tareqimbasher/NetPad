using System.Text.Json.Serialization;

namespace NetPad.Configuration;

public class EditorOptions : ISettingsOptions
{
    public EditorOptions()
    {
        DefaultMissingValues();
    }

    [JsonInclude] public object MonacoOptions { get; private set; } = null!;
    [JsonInclude] public VimOptions Vim { get; private set; } = null!;

    public EditorOptions SetMonacoOptions(object monacoOptions)
    {
        MonacoOptions = monacoOptions;
        return this;
    }

    public EditorOptions SetVimOptions(VimOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        Vim.SetEnabled(options.Enabled);
        return this;
    }

    public void DefaultMissingValues()
    {
        // ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract

        MonacoOptions ??= new
        {
            cursorBlinking = "smooth",
            lineNumbers = "on",
            wordWrap = "off",
            mouseWheelZoom = true,
            minimap = new
            {
                enabled = true
            },
            themeCustomizations = new
            {
                colors = new {},
                rules = Array.Empty<string>()
            }
        };

        (Vim ??= new()).DefaultMissingValues();

        // ReSharper enable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
    }
}

public class VimOptions
{
    [JsonInclude] public bool Enabled { get; private set; }

    public VimOptions SetEnabled(bool enabled)
    {
        Enabled = enabled;
        return this;
    }

    public void DefaultMissingValues()
    {
    }
}
