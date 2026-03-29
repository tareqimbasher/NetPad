using NetPad.Utilities;

namespace NetPad.Runtime.Tests.Utilities;

public class AccessorTests
{
    [Fact]
    public void Constructor_SetsInitialValue()
    {
        var accessor = new Accessor<int>(42);

        Assert.Equal(42, accessor.Value);
    }

    [Fact]
    public void Update_ChangesValue()
    {
        var accessor = new Accessor<string>("initial");

        accessor.Update("updated");

        Assert.Equal("updated", accessor.Value);
    }

    [Fact]
    public void Update_ReturnsSameInstance_ForFluentChaining()
    {
        var accessor = new Accessor<int>(0);

        var returned = accessor.Update(5);

        Assert.Same(accessor, returned);
    }

    [Fact]
    public void Update_SupportsChaining()
    {
        var accessor = new Accessor<int>(0);

        accessor.Update(5).Update(10).Update(15);

        Assert.Equal(15, accessor.Value);
    }

    [Fact]
    public void WorksWithNullableReferenceTypes()
    {
        var accessor = new Accessor<string?>(null);

        Assert.Null(accessor.Value);

        accessor.Update("value");
        Assert.Equal("value", accessor.Value);
    }
}
