using System;
using System.Collections;
using System.Linq;
using System.Reflection;
#if NET6_0_OR_GREATER
using System.Text.Json;
#endif
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class CollectionHtmlConverter : HtmlConverter
{
    public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope,
        HtmlSerializer htmlSerializer)
    {
        return WriteHtmlPrivate(obj, type, serializationScope, htmlSerializer).element;
    }

    public override void WriteHtmlWithinTableRow<T>(Element tr, T obj, Type type, SerializationScope serializationScope,
        HtmlSerializer htmlSerializer)
    {
        var td = tr.AddAndGetElement("td").WithAddClass(htmlSerializer.SerializerSettings.CssClasses.PropertyValue);

        if (obj == null)
        {
            td.AddAndGetNull().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.Null);
            return;
        }

        var result = WriteHtmlPrivate(obj, type, serializationScope, htmlSerializer);

        if (!htmlSerializer.SerializerSettings.DoNotSerializeNonRootEmptyCollections || result.collectionLength != 0)
        {
            td.AddChild(result.element);
        }
        else
        {
            td.AddChild(
                new EmptyCollection(type).WithAddClass(htmlSerializer.SerializerSettings.CssClasses.EmptyCollection));
        }
    }

    public override bool CanConvert(HtmlSerializer htmlSerializer, Type type)
    {
        return htmlSerializer.GetTypeCategory(type) == TypeCategory.Collection;
    }

    private (Element element, int? collectionLength) WriteHtmlPrivate<T>(T obj, Type type,
        SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj == null)
            return (new Null().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.Null), null);

        if (serializationScope.CheckAddAddIsAlreadySerialized(obj))
        {
            var referenceLoopHandling = htmlSerializer.SerializerSettings.ReferenceLoopHandling;

            if (referenceLoopHandling == ReferenceLoopHandling.IgnoreAndSerializeCyclicReference)
                return (
                    new CyclicReference(type).WithAddClass(htmlSerializer.SerializerSettings.CssClasses
                        .CyclicReference), null);

            if (referenceLoopHandling == ReferenceLoopHandling.Ignore)
                return (new Element("div"), null);

            if (referenceLoopHandling == ReferenceLoopHandling.Error)
                throw new HtmlSerializationException(
                    $"A reference loop was detected. Object already serialized: {type.FullName}");
        }

        Type elementType = htmlSerializer.GetElementType(type) ?? typeof(object);

        var table = new Table();

        int collectionLength = 0;

        var enumerable = ToEnumerable(obj);
        var collection = htmlSerializer.SerializerSettings.MaxIQueryableSerializeLength >= 0 && obj is IQueryable
            ? enumerable.Cast<object?>().AsQueryable().Take(1000)
            : enumerable;

        foreach (var item in collection)
        {
            collectionLength++;
            var tr = table.Body.AddAndGetElement("tr");
            htmlSerializer.SerializeWithinTableRow(tr, item, elementType, serializationScope);
        }

        string headerRowText = GetHeaderRowText(collection, type, collectionLength);

        if (htmlSerializer.GetTypeCategory(elementType) == TypeCategory.SingleObject)
        {
            var properties = htmlSerializer.GetReadableProperties(elementType);

            if (properties.Any())
            {
                foreach (var property in properties)
                {
                    table.AddAndGetHeading(property.Name, property.PropertyType.GetReadableName(true, true))
                        .WithAddClass(htmlSerializer.SerializerSettings.CssClasses.PropertyName);
                }

                table.Head.ChildElements.Single().WithAddClass("table-data-header");
            }

            var countHeaderRow = table.Head.InsertAndGetChild(0, new Element("tr"));

            countHeaderRow
                .AddAndGetElement("th")
                .WithAddClass("table-info-header")
                .SetOrAddAttribute("colspan", properties.Length.ToString()).Element
                .AddText(headerRowText);
        }
        else
        {
            table.AddAndGetHeading(headerRowText);
        }

        return (table, collectionLength);
    }

    private string GetHeaderRowText(IEnumerable collection, Type collectionType, int collectionLength)
    {
        string headerRowText = "";

        var collectionTypeName = collectionType.GetReadableName(forHtml: true);

        if (collectionTypeName.StartsWith("IGrouping" + Consts.HtmlLessThan))
        {
            var keyProp = collectionType.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public);
            if (keyProp != null)
            {
                object? keyValue = keyProp.GetValue(collection);

                string? keyValueStr = null;

#if NET6_0_OR_GREATER
                keyValueStr = JsonSerializer.Serialize(keyValue);
#else
                keyValueStr = keyValue?.ToString();
#endif

                keyValueStr = keyValueStr == null
                    ? "(null)"
                    : keyValueStr.Length <= 50
                        ? keyValueStr
                        : keyValueStr.Substring(0, 50);

                headerRowText += "Key = "
                                 + keyValueStr
                                 + Consts.HtmlSpace
                                 + Consts.HtmlSpace
                                 + Consts.HtmlSpace
                                 + Consts.HtmlSpace;
            }
        }

        headerRowText += $"{collectionTypeName} ({collectionLength} items)";

        return headerRowText;
    }

    private IEnumerable ToEnumerable<T>(T obj)
    {
        return obj as IEnumerable ??
               throw new InvalidCastException($"Cannot cast {nameof(obj)} to {nameof(IEnumerable)}");
    }
}
