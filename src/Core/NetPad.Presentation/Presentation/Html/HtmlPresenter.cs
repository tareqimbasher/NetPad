using NetPad.Media;
using O2Html;
using O2Html.Dom;

namespace NetPad.Presentation.Html;

public static class HtmlPresenter
{
    public static readonly HtmlSerializerSettings _htmlSerializerSettings;

    static HtmlPresenter()
    {

        _htmlSerializerSettings = new()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.IgnoreAndSerializeCyclicReference,
            DoNotSerializeNonRootEmptyCollections = true,
            Converters =
            {
                new ImageHtmlConverter(),
                new AudioHtmlConverter(),
                new VideoHtmlConverter(),
                new MediaFileCollectionConverter()
            },
        };

        var configFileValues = PresentationSettings.GetConfigFileValues();

        UpdateSerializerSettings(configFileValues.maxDepth, configFileValues.maxCollectionSerializeLength);
    }

    public static bool IsDotNetTypeWithStringRepresentation(Type type) => HtmlSerializer.IsDotNetTypeWithStringRepresentation(type);

    public static void UpdateSerializerSettings(uint? maxDepth, uint? maxCollectionSerializeLength)
    {
        _htmlSerializerSettings.MaxDepth = maxDepth ?? PresentationSettings.MaxDepth;
        _htmlSerializerSettings.MaxCollectionSerializeLength = maxCollectionSerializeLength ?? PresentationSettings.MaxCollectionLength;
    }

    /// <summary>
    /// Serializes output to an HTML <see cref="Element"/>.
    /// </summary>
    /// <param name="output">The object to serialize.</param>
    /// <param name="title">If provided, will add a title heading to the result.</param>
    /// <param name="isError">
    /// If true, output will be considered an error. This has no effect if <see cref="output"/> is
    /// an <see cref="Exception"/> as</param> as exceptions are always considered errors.
    /// <param name="appendNewLineForAllTextOutput">If true and the output is all text (not a nested structure), a HTML break will be appended to the result.</param>
    /// <returns>An HTML <see cref="Element"/> representing the <see cref="output"/>.</returns>
    public static Element SerializeToElement(
        object? output,
        string? title = null,
        bool isError = false,
        bool appendNewLineForAllTextOutput = false)
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
            node = HtmlConvert.Serialize("Could not serialize object to HTML. " + ex, _htmlSerializerSettings);
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

        group.AddChild(node);

        if (outputIsAllText)
        {
            group.WithAddClass("text");

            if (appendNewLineForAllTextOutput)
            {
                group.AddElement("<br/>");
            }
        }
        else if (output is Image)
        {
            group.WithAddClass("image");
        }
        else if (output is Audio)
        {
            group.WithAddClass("audio");
        }
        else if (output is Video)
        {
            group.WithAddClass("video");
        }

        return group;
    }

    /// <summary>
    /// Serializes output to an HTML string.
    /// </summary>
    /// <param name="output">The object to serialize.</param>
    /// <param name="title">If provided, will add a title heading to the result.</param>
    /// <param name="isError">
    /// If true, output will be considered an error. This has no effect if <see cref="output"/> is
    /// an <see cref="Exception"/> as</param> as exceptions are always considered errors.
    /// <param name="appendNewLineForAllTextOutput">If true and the output is all text (not a nested structure), a HTML break will be appended to the result.</param>
    /// <returns>An HTML string representation of <see cref="output"/>.</returns>
    public static string Serialize(
        object? output,
        string? title = null,
        bool isError = false,
        bool appendNewLineForAllTextOutput = false)
    {
        return SerializeToElement(
            output,
            title,
            isError,
            appendNewLineForAllTextOutput
        ).ToHtml();
    }
}
