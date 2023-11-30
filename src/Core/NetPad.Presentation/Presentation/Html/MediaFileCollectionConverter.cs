using System.Collections;
using NetPad.Media;
using O2Html;
using O2Html.Common;
using O2Html.Converters;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace NetPad.Presentation.Html;

public class MediaFileCollectionConverter : CollectionHtmlConverter
{
    public override bool CanConvert(HtmlSerializer htmlSerializer, Type type)
    {
        if (!typeof(IEnumerable).IsAssignableFrom(type))
        {
            return false;
        }

        var itemType = htmlSerializer.GetCollectionElementType(type);

        return typeof(MediaFile).IsAssignableFrom(itemType);
    }

    protected override (Node node, int? collectionLength) Convert<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (ShouldShortCircuit(obj, type, serializationScope, htmlSerializer, out var shortCircuitValue))
        {
            return (shortCircuitValue, null);
        }

        var enumerable = ToEnumerable(obj);

        var table = new Table();

        var enumerationResult = LazyEnumerable.Enumerate(enumerable, htmlSerializer.SerializerSettings.MaxCollectionSerializeLength, (element, _) =>
        {
            var tr = table.Body.AddAndGetElement("tr");

            htmlSerializer.SerializeWithinTableRow(tr, element, element?.GetType() ?? typeof(MediaFile), serializationScope);

            if (!tr.Children.Any()) table.Body.RemoveChild(tr);
        });

        string headerRowText = GetHeaderRowText(
            enumerable,
            type,
            enumerationResult.ElementsEnumerated,
            enumerationResult.CollectionLengthExceedsMax,
            htmlSerializer);

        table.Head.WithHeading(headerRowText);
        table.Head.ChildElements.Single().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.TableInfoHeader);

        return (table, enumerationResult.ElementsEnumerated);
    }
}
