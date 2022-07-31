using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using NetPad.Common;

namespace NetPad.Configuration;

public class EditorOptions : ISettingsOptions
{
    public EditorOptions()
    {
        DefaultMissingValues();
    }

    [JsonInclude] public string? BackgroundColor { get; private set; }
    [JsonInclude] public CodeCompletionOptions CodeCompletion { get; private set; }
    [JsonInclude] public object MonacoOptions { get; private set; }

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

        if (CodeCompletion == null)
            CodeCompletion = new CodeCompletionOptions
            {
                Enabled = true,
                Provider = new OmniSharpCodeCompletionProviderOptions()
            };

        if (MonacoOptions == null)
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

        // ReSharper enable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
    }
}

public class CodeCompletionOptions
{
    public bool Enabled { get; set; }
    public CodeCompletionProviderOptions? Provider { get; set; }
}

[Newtonsoft.Json.JsonConverter(typeof(NJsonSchema.Converters.JsonInheritanceConverter), "type")]
[JsonConverterWithCtorArgs(typeof(JsonInheritanceConverter<CodeCompletionProviderOptions>), "type")]
[KnownType(typeof(OmniSharpCodeCompletionProviderOptions))]
public abstract class CodeCompletionProviderOptions
{
    protected CodeCompletionProviderOptions(string name)
    {
        Name = name;
    }

    public string? Name { get; set; }
}

public class OmniSharpCodeCompletionProviderOptions : CodeCompletionProviderOptions
{
    public OmniSharpCodeCompletionProviderOptions() : base("OmniSharp")
    {
    }

    public string? ExecutablePath { get; set; }
}
