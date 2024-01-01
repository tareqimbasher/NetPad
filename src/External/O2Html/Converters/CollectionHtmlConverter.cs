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
    public override bool CanConvert(Type type)
    {
        return HtmlSerializer.GetTypeCategory(type) == TypeCategory.Collection;
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
        var td = tr.AddAndGetElement("td").AddClass(htmlSerializer.SerializerOptions.CssClasses.PropertyValue);

        if (obj == null)
        {
            td.AddAndGetNull().AddClass(htmlSerializer.SerializerOptions.CssClasses.Null);
            return;
        }

        var result = Convert(obj, type, serializationScope, htmlSerializer);

        if (result.collectionLength == 0 && htmlSerializer.SerializerOptions.DoNotSerializeNonRootEmptyCollections)
        {
            td.AddChild(new EmptyCollection(type).AddClass(htmlSerializer.SerializerOptions.CssClasses.EmptyCollection));
        }
        else
        {
            td.AddChild(result.node);
        }
    }

    protected virtual (Node node, int? collectionLength) Convert<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        Type elementType = HtmlSerializer.GetCollectionElementType(type) ?? typeof(object);
        var enumerable = ToEnumerable(obj);

        var table = new Table();

        var enumerationResult = Enumerate.Max(enumerable, htmlSerializer.SerializerOptions.MaxCollectionSerializeLength, (item, _) =>
        {
            var tr = table.Body.AddAndGetElement("tr");

            htmlSerializer.SerializeWithinTableRow(tr, item, elementType, serializationScope);

            if (!tr.Children.Any()) table.Body.RemoveChild(tr);
        });

        string headerRowText = GetHeaderRowText(
            enumerable,
            type,
            enumerationResult.ItemsProcessed,
            enumerationResult.CollectionLengthExceedsMax);

        if (HtmlSerializer.GetTypeCategory(elementType) == TypeCategory.SingleObject)
        {
            var properties = HtmlSerializer.GetReadableProperties(elementType);

            if (properties.Any())
            {
                foreach (var property in properties)
                {
                    table.Head
                        .AddAndGetHeading(property.Name, property.PropertyType.GetReadableName(true))
                        .AddClass(htmlSerializer.SerializerOptions.CssClasses.PropertyName);
                }

                table.Head.ChildElements.Single().AddClass(htmlSerializer.SerializerOptions.CssClasses.TableDataHeader);
            }

            var infoHeaderRow = table.Head.InsertAndGetChild(0, new Element("tr"));

            infoHeaderRow
                .AddClass(htmlSerializer.SerializerOptions.CssClasses.TableInfoHeader)
                .SetTitle(type.GetReadableName(true))
                .AddAndGetElement("th")
                .SetAttribute("colspan", properties.Length.ToString())
                .AddEscapedText(headerRowText);
        }
        else
        {
            table.Head.AddHeading(headerRowText);
            table.Head.ChildElements.Single().AddClass(htmlSerializer.SerializerOptions.CssClasses.TableInfoHeader);
        }

        return (table, enumerationResult.ItemsProcessed);
    }

    protected string GetHeaderRowText(
        IEnumerable collection,
        Type collectionType,
        int collectionLength,
        bool collectionHasMoreElementsThanMax)
    {
        string headerRowText = "";

        var collectionTypeName = collectionType.GetReadableName();

        if (collectionType.Namespace == "System.Linq" && collectionTypeName.StartsWith("IGrouping<"))
        {
            // TODO This needs to be simplified
            // Get the type TKey of the IGrouping<TKey, TElement> and add it to the headerRowText

            var keyProp = collectionType.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public);
            if (keyProp != null)
            {
                object? keyValue = keyProp.GetValue(collection);

                string? keyValueStr = null;

                if (keyValue != null)
                {
                    var keyValueType = keyValue.GetType();
                    var typeCategory = HtmlSerializer.GetTypeCategory(keyValueType);

                    if (typeCategory == TypeCategory.DotNetTypeWithStringRepresentation)
                    {
                        keyValueStr = keyValue.ToString();
                    }
                    else if( typeCategory == TypeCategory.Collection)
                    {
                        keyValueStr = keyValue.GetType().GetReadableName();
                    }
                    else if (typeCategory == TypeCategory.SingleObject)
                    {
                        var properties = HtmlSerializer.GetReadableProperties(keyValueType);
                        keyValueStr += "{";
                        for (var iProp = 0; iProp < properties.Length; iProp++)
                        {
                            var property = properties[iProp];
                            var propValue = property.GetValue(keyValue);
                            string? propValueStr = null;
                            if (propValue != null)
                            {
                                var propValueTypeCategory = HtmlSerializer.GetTypeCategory(property.PropertyType);
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
               throw new HtmlSerializationException($"Value of type {obj!.GetType()} is not an {nameof(IEnumerable)}.");
    }
}
