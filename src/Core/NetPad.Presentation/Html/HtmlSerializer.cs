using O2Html;
using O2Html.Dom;

namespace NetPad.Html;

public static class HtmlSerializer
{
    private static readonly HtmlSerializerSettings _htmlSerializerSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.IgnoreAndSerializeCyclicReference,
        DoNotSerializeNonRootEmptyCollections = true,
    };

    public static string Serialize(object? output, string? title = null)
    {
        bool titled = title != null;
        bool outputIsException = output is Exception;

        Node node;

        try
        {
            node = HtmlConvert.Serialize(output, _htmlSerializerSettings);
        }
        catch (Exception ex)
        {
            node = HtmlConvert.Serialize(ex, _htmlSerializerSettings);
            outputIsException = true;
        }

        bool outputIsAllText = node is TextNode || (node is Element element && element.Children.All(c => c.Type == NodeType.Text));

        var group = new Element("div").WithAddClass("group");

        if (outputIsException)
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
        else if (outputIsAllText)
        {
            group.WithAddClass("text");
        }

        group.AddChild(node);

        return group.ToHtml();
    }
}
