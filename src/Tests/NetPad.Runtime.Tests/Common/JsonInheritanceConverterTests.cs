using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using NetPad.Common;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NetPad.Runtime.Tests.Common;

public class JsonInheritanceConverterTests
{
    // --- Constructor / configuration ---

    [Fact]
    public void DefaultDiscriminatorName()
    {
        var converter = new JsonInheritanceConverter<Animal>();

        Assert.Equal("discriminator", converter.DiscriminatorName);
    }

    [Fact]
    public void CustomDiscriminatorName()
    {
        var converter = new JsonInheritanceConverter<Animal>("$type");

        Assert.Equal("$type", converter.DiscriminatorName);
    }

    [Fact]
    public void BaseTypeAndDiscriminator_ArePreserved()
    {
        var converter = new JsonInheritanceConverter<Animal>(typeof(Animal), "$kind");

        Assert.Equal("$kind", converter.DiscriminatorName);
        Assert.True(converter.CanConvert(typeof(Dog)));
        Assert.False(converter.CanConvert(typeof(string)));
    }

    // --- CanConvert ---

    [Fact]
    public void CanConvert_WithBaseType_ReturnsTrueForDerivedType()
    {
        var converter = new JsonInheritanceConverter<Animal>(typeof(Animal));

        Assert.True(converter.CanConvert(typeof(Animal)));
        Assert.True(converter.CanConvert(typeof(Dog)));
        Assert.True(converter.CanConvert(typeof(Cat)));
    }

    [Fact]
    public void CanConvert_WithBaseType_ReturnsFalseForUnrelatedType()
    {
        var converter = new JsonInheritanceConverter<Animal>(typeof(Animal));

        Assert.False(converter.CanConvert(typeof(string)));
    }

    [Fact]
    public void CanConvert_WithoutBaseType_ReturnsTrueForAny()
    {
        var converter = new JsonInheritanceConverter<Animal>();

        Assert.True(converter.CanConvert(typeof(string)));
    }

    // --- GetDiscriminatorValue ---

    [Fact]
    public void GetDiscriminatorValue_ReturnsTypeName()
    {
        var converter = new JsonInheritanceConverter<Animal>();

        Assert.Equal("Dog", converter.GetDiscriminatorValue(typeof(Dog)));
        Assert.Equal("Cat", converter.GetDiscriminatorValue(typeof(Cat)));
    }

    // --- Serialization ---

    [Fact]
    public void Serialize_WritesDerivedTypeProperties_AndDiscriminator()
    {
        var dog = new Dog { Name = "Rex", Breed = "Labrador" };
        var cat = new Cat { Name = "Whiskers", Indoor = true };

        var json = JsonSerializer.Serialize(new Animal[] { dog, cat });
        using var doc = JsonDocument.Parse(json);
        var elements = doc.RootElement;

        Assert.Equal(2, elements.GetArrayLength());

        // Dog
        Assert.Equal("Dog", elements[0].GetProperty("discriminator").GetString());
        Assert.Equal("Rex", elements[0].GetProperty("Name").GetString());
        Assert.Equal("Labrador", elements[0].GetProperty("Breed").GetString());

        // Cat
        Assert.Equal("Cat", elements[1].GetProperty("discriminator").GetString());
        Assert.Equal("Whiskers", elements[1].GetProperty("Name").GetString());
        Assert.True(elements[1].GetProperty("Indoor").GetBoolean());
    }

    [Fact]
    public void Serialize_NullPropertyValues_ArePreserved()
    {
        var dog = new Dog { Name = null!, Breed = null! };
        var json = JsonSerializer.Serialize<Animal>(dog);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Name").ValueKind);
    }

    [Fact]
    public void Serialize_WithNestedObjects_PreservesStructure()
    {
        var dog = new Dog { Name = "Rex", Breed = "Lab", Tag = new PetTag { Id = 42, Material = "Metal" } };
        var json = JsonSerializer.Serialize<Animal>(dog);
        using var doc = JsonDocument.Parse(json);

        var tag = doc.RootElement.GetProperty("Tag");
        Assert.Equal(42, tag.GetProperty("Id").GetInt32());
        Assert.Equal("Metal", tag.GetProperty("Material").GetString());
    }

    [Fact]
    public void Serialize_WithArrayProperty_PreservesElements()
    {
        var dog = new Dog { Name = "Rex", Breed = "Lab", Tricks = ["sit", "stay", "roll"] };
        var json = JsonSerializer.Serialize<Animal>(dog);
        using var doc = JsonDocument.Parse(json);

        var tricks = doc.RootElement.GetProperty("Tricks");
        Assert.Equal(3, tricks.GetArrayLength());
        Assert.Equal("sit", tricks[0].GetString());
    }

    [Fact]
    public void Serialize_WithoutConverter_LosesDerivedTypeProperties()
    {
        var o1 = new PlainDerived1();
        var o2 = new PlainDerived2();

        var json = JsonSerializer.Serialize(new PlainBase[] { o1, o2 });
        using var doc = JsonDocument.Parse(json);

        // Without the converter, no discriminator and no derived properties
        Assert.False(doc.RootElement[0].TryGetProperty("discriminator", out _));
        Assert.False(doc.RootElement[0].TryGetProperty("Derived1Prop", out _));
    }

    // --- Deserialization ---

    [Fact]
    public void Deserialize_ToAbstractBaseType_ResolvesDerivedType()
    {
        var json = """{"discriminator": "Dog", "Name": "Buddy", "Breed": "Poodle"}""";

        var result = JsonSerializer.Deserialize<Animal>(json);

        var dog = Assert.IsType<Dog>(result);
        Assert.Equal("Buddy", dog.Name);
        Assert.Equal("Poodle", dog.Breed);
    }

    [Fact]
    public void Deserialize_NullValue_ReturnsNull()
    {
        var result = JsonSerializer.Deserialize<Animal>("null");

        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_UnknownDiscriminator_ThrowsInvalidOperationException()
    {
        var json = """{"discriminator": "UnknownType", "Name": "Test"}""";

        Assert.Throws<InvalidOperationException>(() =>
            JsonSerializer.Deserialize<Animal>(json));
    }

    [Fact]
    public void Deserialize_WithoutConverter_ThrowsForAbstractBaseType()
    {
        var json = """{"discriminator": "PlainDerived1", "BaseProperty": "123", "Derived1Prop": "456"}""";

        Assert.Throws<NotSupportedException>(() =>
            JsonSerializer.Deserialize<PlainBase>(json));
    }

    [Fact]
    public void Deserialize_WithoutConverter_WorksForConcreteType()
    {
        var json = """{"BaseProperty": "123", "Derived1Prop": "456"}""";

        var result = JsonSerializer.Deserialize<PlainDerived1>(json);

        Assert.NotNull(result);
        Assert.Equal("123", result!.BaseProperty);
        Assert.Equal("456", result.Derived1Prop);
    }

    // --- Round-trip ---

    [Fact]
    public void RoundTrip_PreservesConcreteType()
    {
        var original = new Dog { Name = "Buddy", Breed = "Labrador" };

        var json = JsonSerializer.Serialize<Animal>(original);
        var deserialized = JsonSerializer.Deserialize<Animal>(json);

        var dog = Assert.IsType<Dog>(deserialized);
        Assert.Equal("Buddy", dog.Name);
        Assert.Equal("Labrador", dog.Breed);
    }

    [Fact]
    public void RoundTrip_WorksWithMultipleSubtypes()
    {
        Animal[] animals = [new Dog { Name = "Rex", Breed = "Shepherd" }, new Cat { Name = "Whiskers", Indoor = true }];

        var json = JsonSerializer.Serialize(animals);
        var deserialized = JsonSerializer.Deserialize<Animal[]>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized!.Length);
        Assert.IsType<Dog>(deserialized[0]);
        Assert.IsType<Cat>(deserialized[1]);
        Assert.Equal("Shepherd", ((Dog)deserialized[0]).Breed);
        Assert.True(((Cat)deserialized[1]).Indoor);
    }

    // --- Test types: with converter ---

    [JsonConverter(typeof(JsonInheritanceConverter<Animal>))]
    [KnownType(typeof(Dog))]
    [KnownType(typeof(Cat))]
    private abstract class Animal
    {
        public string Name { get; set; } = "";
    }

    private class Dog : Animal
    {
        public string Breed { get; set; } = "";
        public PetTag? Tag { get; set; }
        public List<string> Tricks { get; set; } = [];
    }

    private class Cat : Animal
    {
        public bool Indoor { get; set; }
    }

    private class PetTag
    {
        public int Id { get; set; }
        public string Material { get; set; } = "";
    }

    // --- Test types: without converter (for comparison tests) ---

    private abstract class PlainBase
    {
        public string BaseProperty { get; set; } = "Base Property Value";
    }

    private class PlainDerived1 : PlainBase
    {
        public string Derived1Prop { get; set; } = "Derived 1 Value";
    }

    private class PlainDerived2 : PlainBase
    {
        public string Derived2Prop { get; set; } = "Derived 2 Value";
    }
}
