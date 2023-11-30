using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using O2Html.Common;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class CollectionHtmlConverter : HtmlConverter
{
    public override bool CanConvert(HtmlSerializer htmlSerializer, Type type)
    {
        return htmlSerializer.GetTypeCategory(type) == TypeCategory.Collection;
    }

    public override Node WriteHtml<T>(
        T obj,
        Type type,
        SerializationScope serializationScope,
        HtmlSerializer htmlSerializer)
    {
        return Convert(obj, type, serializationScope, htmlSerializer).node;
    }

    public override void WriteHtmlWithinTableRow<T>(
        Element tr,
        T obj,
        Type type,
        SerializationScope serializationScope,
        HtmlSerializer htmlSerializer)
    {
        var td = tr.AddAndGetElement("td").WithAddClass(htmlSerializer.SerializerSettings.CssClasses.PropertyValue);

        if (obj == null)
        {
            td.AddAndGetNull().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.Null);
            return;
        }

        var result = Convert(obj, type, serializationScope, htmlSerializer);

        if (result.collectionLength == 0 && htmlSerializer.SerializerSettings.DoNotSerializeNonRootEmptyCollections)
        {
            td.AddChild(new EmptyCollection(type).WithAddClass(htmlSerializer.SerializerSettings.CssClasses.EmptyCollection));
        }
        else
        {
            td.AddChild(result.node);
        }
    }

    protected virtual (Node node, int? collectionLength) Convert<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj == null)
            return (new Null(htmlSerializer.SerializerSettings.CssClasses.Null), null);

        if (ShouldShortCircuit(obj, type, serializationScope, htmlSerializer, out var shortCircuitValue))
        {
            return (shortCircuitValue, null);
        }

        Type elementType = htmlSerializer.GetCollectionElementType(type) ?? typeof(object);
        var enumerable = ToEnumerable(obj);

        var table = new Table();

        var enumerationResult = LazyEnumerable.Enumerate(enumerable, htmlSerializer.SerializerSettings.MaxCollectionSerializeLength, (element, _) =>
        {
            var tr = table.Body.AddAndGetElement("tr");

            htmlSerializer.SerializeWithinTableRow(tr, element, elementType, serializationScope);

            if (!tr.Children.Any()) table.Body.RemoveChild(tr);
        });

        string headerRowText = GetHeaderRowText(
            enumerable,
            type,
            enumerationResult.ElementsEnumerated,
            enumerationResult.CollectionLengthExceedsMax,
            htmlSerializer);

        if (htmlSerializer.GetTypeCategory(elementType) == TypeCategory.SingleObject)
        {
            var properties = htmlSerializer.GetReadableProperties(elementType);

            if (properties.Any())
            {
                foreach (var property in properties)
                {
                    table.Head
                        .AddAndGetHeading(property.Name, property.PropertyType.GetReadableName(true))
                        .WithAddClass(htmlSerializer.SerializerSettings.CssClasses.PropertyName);
                }

                table.Head.ChildElements.Single().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.TableDataHeader);
            }

            var infoHeaderRow = table.Head.InsertAndGetChild(0, new Element("tr"));

            infoHeaderRow
                .WithAddClass(htmlSerializer.SerializerSettings.CssClasses.TableInfoHeader)
                .WithTitle(type.GetReadableName(true))
                .AddAndGetElement("th")
                .SetOrAddAttribute("colspan", properties.Length.ToString()).Element
                .AddText(headerRowText);
        }
        else
        {
            table.Head.WithHeading(headerRowText);
            table.Head.ChildElements.Single().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.TableInfoHeader);
        }

        return (table, enumerationResult.ElementsEnumerated);
    }

    protected string GetHeaderRowText(
        IEnumerable collection,
        Type collectionType,
        int collectionLength,
        bool collectionHasMoreElementsThanMax,
        HtmlSerializer htmlSerializer)
    {
        string headerRowText = "";

        var collectionTypeName = collectionType.GetReadableName();

        if (collectionType.Namespace == "System.Linq" && collectionTypeName.StartsWith("IGrouping<"))
        {
            var keyProp = collectionType.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public);
            if (keyProp != null)
            {
                object? keyValue = keyProp.GetValue(collection);

                string? keyValueStr = null;

                if (keyValue != null)
                {
                    var keyValueType = keyValue.GetType();
                    var typeCategory = htmlSerializer.GetTypeCategory(keyValueType);
                    if (typeCategory == TypeCategory.DotNetTypeWithStringRepresentation)
                    {
                        keyValueStr = keyValue.ToString();
                    }
                    else if (typeCategory == TypeCategory.SingleObject)
                    {
                        var properties = htmlSerializer.GetReadableProperties(keyValueType);
                        keyValueStr += "{";
                        for (var iProp = 0; iProp < properties.Length; iProp++)
                        {
                            var property = properties[iProp];
                            var propValue = property.GetValue(keyValue);
                            string? propValueStr = null;
                            if (propValue != null)
                            {
                                var propValueTypeCategory = htmlSerializer.GetTypeCategory(property.PropertyType);
                                propValueStr = propValueTypeCategory == TypeCategory.DotNetTypeWithStringRepresentation
                                    ? propValue.ToString()
                                    : property.PropertyType.GetReadableName();
                            }

                            propValueStr ??= "(null)";

                            if (iProp > 0) keyValueStr += ", ";
                            keyValueStr += $"{property.Name}: {propValueStr}";
                        }

                        keyValueStr += "}";
                    }
                    else
                    {
                        keyValueStr = keyValue.GetType().GetReadableName();
                    }
                }

                keyValueStr = keyValueStr == null
                    ? "(null)"
                    : keyValueStr.Length <= 50
                        ? keyValueStr
                        : keyValueStr.Substring(0, 50);

                headerRowText += $"Key = {keyValueStr}    ";
            }
        }

        headerRowText += $"{collectionTypeName} ({(collectionHasMoreElementsThanMax ? "First " : "")}{collectionLength} items)";

        return headerRowText;
    }

    protected IEnumerable ToEnumerable<T>(T obj)
    {
        return obj as IEnumerable ??
               throw new InvalidCastException($"Cannot cast {nameof(obj)} of type {obj!.GetType()} to {nameof(IEnumerable)}");
    }
}
