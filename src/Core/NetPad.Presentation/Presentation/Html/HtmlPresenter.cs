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
    /// <param name="options">Dump options</param>
    /// <param name="isError">
    /// If true, output will be considered an error. This has no effect if <see cref="output"/> is
    /// an <see cref="Exception"/> as</param> as exceptions are always considered errors.
    /// <returns>An HTML <see cref="Element"/> representing the <see cref="output"/>.</returns>
    public static Element SerializeToElement(
        object? output,
        DumpOptions? options = null,
        bool isError = false)
    {
        options ??= DumpOptions.Default;

        bool isTitled = options.Title != null;

        if (!isError && output is Exception)
        {
            isError = true;
        }

        if (output is not Node node)
        {
            try
            {
                node = HtmlSerializer.Serialize(output, _htmlSerializerSettings);
            }
            catch (Exception ex)
            {
                node = HtmlSerializer.Serialize("Could not serialize object to HTML. " + ex, _htmlSerializerSettings);
                isError = true;
            }
        }

        bool outputIsAllText = node is TextNode { IsEscaped: true } ||
                               (node is Element element && element.Children.Any() && element.Children.All(c => c.Type == NodeType.Text));

        var group = new Element("div").AddClass("group");

        if (options.CssClasses?.Length > 0)
        {
            group.AddClass(options.CssClasses);
        }

        if (isError)
        {
            group.AddClass("error");
        }

        if (isTitled)
        {
            group.AddClass("titled")
                .AddAndGetElement("h6")
                .AddClass("title")
                .AddText(options.Title);

            if (outputIsAllText)
            {
                node = new Element("span").AddClass("text").AddChild(node);
            }
        }

        group.AddChild(node);

        if (outputIsAllText)
        {
            group.AddClass("text");

            if (options.AppendNewLine)
            {
                group.AddElement("<br/>");
            }
        }
        else if (output is Image)
        {
            group.AddClass("image");
        }
        else if (output is Audio)
        {
            group.AddClass("audio");
        }
        else if (output is Video)
        {
            group.AddClass("video");
        }

        if (options.DestructAfterMs > 0)
        {
            group.SetAttribute("data-destruct", options.DestructAfterMs.Value.ToString());
        }

        return group;
    }

    /// <summary>
    /// Serializes output to an HTML string.
    /// </summary>
    /// <param name="output">The object to serialize.</param>
    /// <param name="options">Dump options</param>
    /// <param name="isError">
    /// If true, output will be considered an error. This has no effect if <see cref="output"/> is
    /// an <see cref="Exception"/> as</param> as exceptions are always considered errors.
    /// <returns>An HTML string representation of <see cref="output"/>.</returns>
    public static string Serialize(
        object? output,
        DumpOptions? options = null,
        bool isError = false
    )
    {
        return SerializeToElement(output, options, isError).ToHtml();
    }
}
