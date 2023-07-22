using System.Reflection;
using System.Text.Json;
using O2Html;
using O2Html.Dom;

namespace NetPad.Html;

public static class HtmlSerializer
{
    public static HtmlSerializerSettings _htmlSerializerSettings;

    static HtmlSerializer()
    {
        var configFileValues = GetConfigFileValues();

        _htmlSerializerSettings = new()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.IgnoreAndSerializeCyclicReference,
            DoNotSerializeNonRootEmptyCollections = true,
            MaxDepth = configFileValues.maxDepth ?? 64,
            MaxCollectionSerializeLength = configFileValues.maxCollectionSerializeLength ?? 1000
        };
    }

    public static bool IsDotNetTypeWithStringRepresentation(Type type) => O2Html.HtmlSerializer.IsDotNetTypeWithStringRepresentation(type);

    public static string Serialize(object? output, string? title = null, bool isError = false)
    {
        bool titled = title != null;

        if (!isError && output is Exception)
        {
            isError = true;
        }

        Node node;

        try
        {
            node = HtmlConvert.Serialize(output, _htmlSerializerSettings);
        }
        catch (Exception ex)
        {
            node = HtmlConvert.Serialize(ex, _htmlSerializerSettings);
            isError = true;
        }

        bool outputIsAllText = node is TextNode || (node is Element element && element.Children.All(c => c.Type == NodeType.Text));

        var group = new Element("div").WithAddClass("group");

        if (isError)
        {
            group.WithAddClass("error");
        }

        if (titled)
        {
            group.WithAddClass("titled")
                .AddAndGetElement("h6")
                .WithAddClass("title")
                .AddText(title);

            if (outputIsAllText)
            {
                node = new Element("span").WithAddClass("text").WithChild(node);
            }
        }

        if (outputIsAllText)
        {
            group.WithAddClass("text");
        }

        group.AddChild(node);

        return group.ToHtml();
    }

    public static void UpdateHtmlSerializerSettings(uint maxDepth, uint maxCollectionSerializeLength)
    {
        _htmlSerializerSettings.MaxDepth = maxDepth;
        _htmlSerializerSettings.MaxCollectionSerializeLength = maxCollectionSerializeLength;
    }

    public static (uint? maxDepth, uint? maxCollectionSerializeLength) GetConfigFileValues()
    {
        var scriptConfigFilePath  = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "scriptconfig.json"
        );

        if (!File.Exists(scriptConfigFilePath)) return (null, null);

        uint? maxDepth = null;
        uint? maxCollectionSerializeLength = null;

        try
        {
            var json = JsonDocument.Parse(File.ReadAllText(scriptConfigFilePath));

            if (!json.RootElement.TryGetProperty("output", out var outputSettings)) return (null, null);

            if (outputSettings.TryGetProperty("maxDepth", out var prop) && prop.TryGetUInt32(out var md))
            {
                maxDepth = md;
            }

            if (outputSettings.TryGetProperty("maxCollectionSerializeLength", out prop) && prop.TryGetUInt32(out md))
            {
                maxCollectionSerializeLength = md;
            }

            return (maxDepth, maxCollectionSerializeLength);
        }
        catch
        {
            return (null, null);
        }
    }
}
