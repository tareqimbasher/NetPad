using System.Linq;
using Xunit;

namespace O2Html.Tests;

public class SerializationScopeTests
{
    [Fact]
    public void CheckAlreadySerializedOrAdd_ReturnsFalse_ForUnseenObject()
    {
        var serializationScope = new SerializationScope(0);
        var person = new Person("John", 30);

        var alreadySerialized = serializationScope.CheckAlreadySerializedOrAdd(person);

        Assert.False(alreadySerialized);
        Assert.Single(serializationScope.SerializedObjects);
        Assert.Same(person, serializationScope.SerializedObjects.Single());
    }

    [Fact]
    public void CheckAlreadySerializedOrAdd_ReturnsTrue_ForSeenObject()
    {
        var serializationScope = new SerializationScope(0);
        var person = new Person("John", 30);
        serializationScope.CheckAlreadySerializedOrAdd(person);

        var alreadySerialized = serializationScope.CheckAlreadySerializedOrAdd(person);

        Assert.True(alreadySerialized);
        Assert.Single(serializationScope.SerializedObjects);
        Assert.Same(person, serializationScope.SerializedObjects.Single());
    }

    [Fact]
    public void CheckAlreadySerializedOrAdd_DoesNotAddSeenObject()
    {
        var serializationScope = new SerializationScope(0);
        var person = new Person("John", 30);
        serializationScope.CheckAlreadySerializedOrAdd(person);

        var alreadySerialized = serializationScope.CheckAlreadySerializedOrAdd(person);

        Assert.True(alreadySerialized);
        Assert.Single(serializationScope.SerializedObjects);
        Assert.Same(person, serializationScope.SerializedObjects.Single());
    }

    [Fact]
    public void CheckAlreadySerializedOrAdd_UsesReferenceEquality()
    {
        var serializationScope = new SerializationScope(0);
        var product1 = new Product("Coffee Mug", 10);
        var product2 = new Product("Coffee Mug", 10);
        serializationScope.CheckAlreadySerializedOrAdd(product1);

        var alreadySerialized = serializationScope.CheckAlreadySerializedOrAdd(product2);

        Assert.False(alreadySerialized, "Considers product2 as unseen");
        Assert.Equal(2, serializationScope.SerializedObjects.Count);
        Assert.Same(product1, serializationScope.SerializedObjects.ElementAt(0));
        Assert.Same(product2, serializationScope.SerializedObjects.ElementAt(1));
    }

    class Person
    {
        public Person(string name, int age)
        {
            Name = name;
            Age = age;
        }

        public string Name { get; }
        public int Age { get; }
    }

    record Product(string Name, decimal Price);
}
