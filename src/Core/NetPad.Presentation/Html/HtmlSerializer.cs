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
        var group = new Element("div").WithAddClass("group");

        if (title != null)
        {
            group.WithAddClass("titled")
                .AddAndGetElement("h6")
                .WithAddClass("title")
                .AddText(title);
        }

        Node node;

        try
        {
            node = HtmlConvert.Serialize(output, _htmlSerializerSettings);
        }
        catch (Exception ex)
        {
            node = HtmlConvert.Serialize(ex, _htmlSerializerSettings);
        }

        if (node is TextNode || (node is Element element && element.Children.All(c => c.Type == NodeType.Text)))
            group.WithAddClass("text");

        if (output is Exception)
            group.WithAddClass("error");

        group.AddChild(node);

        return group.ToHtml();
    }
}
