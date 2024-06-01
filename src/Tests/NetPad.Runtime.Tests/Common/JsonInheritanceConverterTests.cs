using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using NetPad.Common;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NetPad.Runtime.Tests.Common;

public class JsonInheritanceConverterTests
{
    private const string _defaultDiscriminatorName = "discriminator";

    [Fact]
    public void DefaultDiscriminatorName()
    {
        var converter = new JsonInheritanceConverter<BaseTypeUsingInheritanceConverter>();

        Assert.Equal(_defaultDiscriminatorName, converter.DiscriminatorName);
    }

    [Fact]
    public void DiscriminatorName_CanBeSet()
    {
        var discriminatorName = "somename";
        var converter = new JsonInheritanceConverter<BaseTypeUsingInheritanceConverter>(discriminatorName);

        Assert.Equal(discriminatorName, converter.DiscriminatorName);
    }

    [Fact]
    public void WhenUsed_CanSerializeDerivedTypes()
    {
        var o1 =  new DerivedType1UsingInheritanceConverter();
        var o2 =  new DerivedType2UsingInheritanceConverter();

        var array = new BaseTypeUsingInheritanceConverter[] { o1, o2 };
        var jsonStr = JsonSerializer.Serialize(array);
        var json = JsonDocument.Parse(jsonStr).RootElement;
        var element1 = json[0];
        var element2 = json[1];

        Assert.Equal(2, json.GetArrayLength());

        Assert.Equal(nameof(DerivedType1UsingInheritanceConverter), element1.GetProperty(_defaultDiscriminatorName).GetString());
        Assert.Equal(nameof(DerivedType2UsingInheritanceConverter), element2.GetProperty(_defaultDiscriminatorName).GetString());

        Assert.Equal(o1.BaseProperty, element1.GetProperty(nameof(BaseTypeUsingInheritanceConverter.BaseProperty)).GetString());
        Assert.Equal(o2.BaseProperty, element2.GetProperty(nameof(BaseTypeUsingInheritanceConverter.BaseProperty)).GetString());

        Assert.Equal(o1.DerivedType1Property, element1.GetProperty(nameof(DerivedType1UsingInheritanceConverter.DerivedType1Property)).GetString());
        Assert.Equal(o2.DerivedType2Property, element2.GetProperty(nameof(DerivedType2UsingInheritanceConverter.DerivedType2Property)).GetString());
    }

    [Fact]
    public void WhenNotUsed_CanNotSerializeDerivedTypes()
    {
        var o1 =  new DerivedType1NotUsingInheritanceConverter();
        var o2 =  new DerivedType2NotUsingInheritanceConverter();

        var array = new BaseTypeNotUsingInheritanceConverter[] { o1, o2 };
        var jsonStr = JsonSerializer.Serialize(array);
        var json = JsonDocument.Parse(jsonStr).RootElement;
        var element1 = json[0];
        var element2 = json[1];

        Assert.Equal(2, json.GetArrayLength());

        Assert.False(element1.TryGetProperty(_defaultDiscriminatorName, out _));
        Assert.False(element2.TryGetProperty(_defaultDiscriminatorName, out _));

        Assert.Equal(o1.BaseProperty, element1.GetProperty(nameof(BaseTypeNotUsingInheritanceConverter.BaseProperty)).GetString());
        Assert.Equal(o2.BaseProperty, element2.GetProperty(nameof(BaseTypeNotUsingInheritanceConverter.BaseProperty)).GetString());

        Assert.False(element1.TryGetProperty(nameof(DerivedType1NotUsingInheritanceConverter.DerivedType1Property), out _));
        Assert.False(element2.TryGetProperty(nameof(DerivedType2NotUsingInheritanceConverter.DerivedType2Property), out _));
    }

    [Fact]
    public void WhenUsed_CanDeserilaizeToAbstractBaseType()
    {
        var json = $"{{\"{_defaultDiscriminatorName}\": \"{nameof(DerivedType1UsingInheritanceConverter)}\", " +
                   $"\"{nameof(BaseTypeUsingInheritanceConverter.BaseProperty)}\": \"123\", " +
                   $"\"{nameof(DerivedType1UsingInheritanceConverter.DerivedType1Property)}\": \"456\"}}";

        var o = JsonSerializer.Deserialize<BaseTypeUsingInheritanceConverter>(json) as DerivedType1UsingInheritanceConverter;

        Assert.NotNull(o);
        Assert.Equal("123", o!.BaseProperty);
        Assert.Equal("456", o!.DerivedType1Property);
    }

    [Fact]
    public void WhenNotUsed_ThrowsWhenDeserializingToAbstractBaseType()
    {
        var json = $"{{\"{_defaultDiscriminatorName}\": \"{nameof(DerivedType1NotUsingInheritanceConverter)}\", " +
                   $"\"{nameof(BaseTypeNotUsingInheritanceConverter.BaseProperty)}\": \"123\", " +
                   $"\"{nameof(DerivedType1NotUsingInheritanceConverter.DerivedType1Property)}\": \"456\"}}";

        Assert.Throws<NotSupportedException>(() =>
            JsonSerializer.Deserialize<BaseTypeNotUsingInheritanceConverter>(json) as DerivedType1NotUsingInheritanceConverter);
    }

    [Fact]
    public void WhenNotUsed_CanDeserilaizeToDerivedType()
    {
        var json = $"{{\"{_defaultDiscriminatorName}\": \"{nameof(DerivedType1NotUsingInheritanceConverter)}\", " +
                   $"\"{nameof(BaseTypeNotUsingInheritanceConverter.BaseProperty)}\": \"123\", " +
                   $"\"{nameof(DerivedType1NotUsingInheritanceConverter.DerivedType1Property)}\": \"456\"}}";

        var o = JsonSerializer.Deserialize<DerivedType1NotUsingInheritanceConverter>(json);

        Assert.NotNull(o);
        Assert.Equal("123", o!.BaseProperty);
        Assert.Equal("456", o!.DerivedType1Property);
    }

    [JsonConverter(typeof(JsonInheritanceConverter<BaseTypeUsingInheritanceConverter>))]
    [KnownType(typeof(DerivedType1UsingInheritanceConverter))]
    [KnownType(typeof(DerivedType2UsingInheritanceConverter))]
    private abstract class BaseTypeUsingInheritanceConverter
    {
        public string BaseProperty { get; set; } = "Base Property Value";
    }

    private class DerivedType1UsingInheritanceConverter : BaseTypeUsingInheritanceConverter
    {
        public string DerivedType1Property { get; set; } = "Derived Type 1 Property";
    }

    private class DerivedType2UsingInheritanceConverter : BaseTypeUsingInheritanceConverter
    {
        public string DerivedType2Property { get; set; } = "Derived Type 2 Property";
    }

    private abstract class BaseTypeNotUsingInheritanceConverter
    {
        public string BaseProperty { get; set; } = "Base Property Value";
    }

    private class DerivedType1NotUsingInheritanceConverter : BaseTypeNotUsingInheritanceConverter
    {
        public string DerivedType1Property { get; set; } = "Derived Type 1 Property";
    }

    private class DerivedType2NotUsingInheritanceConverter : BaseTypeNotUsingInheritanceConverter
    {
        public string DerivedType2Property { get; set; } = "Derived Type 2 Property";
    }
}
