using O2Html;
using O2Html.Dom;

namespace NetPad.Html;

public static class HtmlSerializer
{
    private static readonly HtmlSerializerSettings _htmlSerializerSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.IgnoreAndSerializeCyclicReference,
        DoNotSerializeNonRootEmptyCollections = true,
        MaxCollectionSerializeLength = 1000
    };

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
}
