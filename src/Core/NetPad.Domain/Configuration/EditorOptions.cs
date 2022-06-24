using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using NetPad.Common;

namespace NetPad.Configuration;

public class EditorOptions
{
    public EditorOptions()
    {
        CodeCompletion = new CodeCompletionOptions();
        MonacoOptions = new object();
    }

    public CodeCompletionOptions CodeCompletion { get; set; }
    public object MonacoOptions { get; set; }
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
