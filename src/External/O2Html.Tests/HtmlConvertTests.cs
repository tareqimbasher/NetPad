using System;
using System.Collections.Generic;
using System.Linq;
using O2Html.Converters;
using O2Html.Dom;
using O2Html.Dom.Elements;
using Xunit;

namespace O2Html.Tests;

public class HtmlConvertTests
{
    private static readonly HtmlSerializerOptions _htmlSerializerOptions = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.IgnoreAndSerializeCyclicReference
    };

    [Theory]
    [MemberData(nameof(TestData.All), MemberType = typeof(TestData))]
    public void HtmlSerailizerSuccessfullyConverts(object? value)
    {
        var node = HtmlSerializer.Serialize(value, _htmlSerializerOptions);
        _ = node.ToHtml();
    }

    public static IEnumerable<object?[]> GetConverterTestData()
    {
        var result = new List<object?[]>();

        void AddData(IEnumerable<object?[]> data, Type converterType)
        {
            foreach (var item in data)
            {
                result!.Add(item.Append(converterType).ToArray());
            }
        }

        AddData(TestData.RepresentedAsStringsValues(), typeof(DotNetTypeWithStringRepresentationHtmlConverter));
        AddData(TestData.ObjectValues(), typeof(ObjectHtmlConverter));
        AddData(TestData.CollectionValues(), typeof(CollectionHtmlConverter));
        AddData(TestData.TwoDimensionalArrayValues(), typeof(TwoDimensionalArrayHtmlConverter));
        AddData(TestData.TupleValues(), typeof(TupleHtmlConverter));
        AddData(TestData.MemoryValues(), typeof(MemoryHtmlConverter));
        AddData(TestData.XmlNodeValues(), typeof(XmlNodeHtmlConverter));
        AddData(TestData.XNodeValues(), typeof(XNodeHtmlConverter));
        AddData(TestData.JsonDocumentValues(), typeof(JsonDocumentHtmlConverter));
        AddData(TestData.FileSystemInfoValues(), typeof(FileSystemInfoHtmlConverter));
        AddData(TestData.DataTableValues(), typeof(DataTableHtmlConverter));
        AddData(TestData.DataSetValues(), typeof(DataSetHtmlConverter));

        return result;
    }

    [Theory]
    [MemberData(nameof(GetConverterTestData))]
    public void HtmlSerailizerGetsCorrectConverter(object? value, Type expectedConverterType)
    {
        var converter = HtmlSerializer.Create().GetConverter(value?.GetType() ?? typeof(object));

        Assert.NotNull(converter);
        Assert.StrictEqual(expectedConverterType, converter.GetType());
    }

    [Theory]
    [InlineData(typeof(FirstSeenByCustomSerializer), true)]
    [InlineData(typeof(FirstSeenByDefaultSerializer), false)]
    public void EachSerializerGetsItsOwnConverterForTheSameType(Type type, bool customSerializerSeesItFirst)
    {
        var customOptions = new HtmlSerializerOptions();
        customOptions.Converters.Add(new SubjectConverter());
        var customSerializer = HtmlSerializer.Create(customOptions);
        var defaultSerializer = HtmlSerializer.Create();

        HtmlConverter? customConverter;
        HtmlConverter? defaultConverter;
        if (customSerializerSeesItFirst)
        {
            customConverter = customSerializer.GetConverter(type);
            defaultConverter = defaultSerializer.GetConverter(type);
        }
        else
        {
            defaultConverter = defaultSerializer.GetConverter(type);
            customConverter = customSerializer.GetConverter(type);
        }

        Assert.IsType<SubjectConverter>(customConverter);
        Assert.IsType<ObjectHtmlConverter>(defaultConverter);
    }

    private class FirstSeenByCustomSerializer
    {
    }

    private class FirstSeenByDefaultSerializer
    {
    }

    private class SubjectConverter : HtmlConverter
    {
        public override bool CanConvert(Type type) =>
            type == typeof(FirstSeenByCustomSerializer) || type == typeof(FirstSeenByDefaultSerializer);

        public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer) =>
            new Element("span");

        public override void WriteHtmlWithinTableRow<T>(TableRow tr, T obj, Type type, SerializationScope serializationScope,
            HtmlSerializer htmlSerializer)
        {
        }
    }
}
