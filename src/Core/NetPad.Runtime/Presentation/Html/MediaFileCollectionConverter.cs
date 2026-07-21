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
    public override bool CanConvert(Type type)
    {
        if (!typeof(IEnumerable).IsAssignableFrom(type))
        {
            return false;
        }

        var itemType = HtmlSerializer.GetCollectionElementType(type);

        return typeof(MediaFile).IsAssignableFrom(itemType);
    }

    protected override (Node node, int? collectionLength) Convert<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        var enumerable = ToEnumerable(obj);

        var table = new Table();

        var enumerationResult = Enumerate.Max(enumerable, htmlSerializer.SerializerOptions.MaxCollectionSerializeLength, (item, _) =>
        {
            var tr = table.Body.AddAndGetRow();

            htmlSerializer.SerializeWithinTableRow(tr, item, item?.GetType() ?? typeof(MediaFile), serializationScope);

            if (!tr.Children.Any()) table.Body.RemoveChild(tr);
        });

        string headerRowText = GetHeaderRowText(enumerable, type);

        table.Head
            .AddAndGetHeading(headerRowText)
            .AddItemCount(htmlSerializer, enumerationResult.ItemsProcessed, "items", enumerationResult.CollectionLengthExceedsMax);

        table.Head.ChildElements.Single().AddClass(htmlSerializer.SerializerOptions.CssClasses.TableInfoHeader);

        return (table, enumerationResult.ItemsProcessed);
    }
}
