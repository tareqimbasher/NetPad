using NetPad.Utilities;

namespace NetPad.Runtime.Tests.Utilities;

public class PropertyChangeDetectorTests
{
    private class SampleObject
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public string? Email { get; set; }
    }

    private class DerivedObject : SampleObject
    {
        public string? Extra { get; set; }
    }

    [Fact]
    public void HasChanges_ReturnsFalse_WhenObjectsAreIdentical()
    {
        var a = new SampleObject { Name = "Alice", Age = 30, Email = "a@b.com" };
        var b = new SampleObject { Name = "Alice", Age = 30, Email = "a@b.com" };

        var result = PropertyChangeDetector.HasChanges(a, b, []);

        Assert.False(result);
    }

    [Fact]
    public void HasChanges_ReturnsTrue_WhenPropertyDiffers()
    {
        var a = new SampleObject { Name = "Alice", Age = 30 };
        var b = new SampleObject { Name = "Bob", Age = 30 };

        var result = PropertyChangeDetector.HasChanges(a, b, []);

        Assert.True(result);
    }

    [Fact]
    public void HasChanges_ReturnsFalse_WhenChangedPropertyIsExcluded()
    {
        var a = new SampleObject { Name = "Alice", Age = 30 };
        var b = new SampleObject { Name = "Bob", Age = 30 };

        var result = PropertyChangeDetector.HasChanges(a, b, ["Name"]);

        Assert.False(result);
    }

    [Fact]
    public void HasChanges_ReturnsTrue_WhenNullBecomesNonNull()
    {
        var a = new SampleObject { Name = null };
        var b = new SampleObject { Name = "Alice" };

        var result = PropertyChangeDetector.HasChanges(a, b, []);

        Assert.True(result);
    }

    [Fact]
    public void HasChanges_ReturnsTrue_WhenNonNullBecomesNull()
    {
        var a = new SampleObject { Name = "Alice" };
        var b = new SampleObject { Name = null };

        var result = PropertyChangeDetector.HasChanges(a, b, []);

        Assert.True(result);
    }

    [Fact]
    public void HasChanges_ReturnsFalse_WhenBothPropertiesAreNull()
    {
        var a = new SampleObject { Name = null, Email = null };
        var b = new SampleObject { Name = null, Email = null };

        var result = PropertyChangeDetector.HasChanges(a, b, []);

        Assert.False(result);
    }

    [Fact]
    public void HasChanges_ThrowsWhenDerivedTypeHasExtraProperties()
    {
        var a = new SampleObject { Name = "Alice" };
        var b = new DerivedObject { Name = "Alice", Extra = "something" };

        // Uses updated's runtime type properties, but existing doesn't have Extra
        Assert.ThrowsAny<Exception>(() => PropertyChangeDetector.HasChanges(a, b, []));
    }

    [Fact]
    public void HasChanges_WorksWithDerivedType_WhenExtraPropsExcluded()
    {
        var a = new SampleObject { Name = "Alice" };
        var b = new DerivedObject { Name = "Alice", Extra = "something" };

        var result = PropertyChangeDetector.HasChanges(a, b, ["Extra"]);

        Assert.False(result);
    }

    [Fact]
    public void HasChanges_ReturnsFalse_WhenAllChangedPropertiesExcluded()
    {
        var a = new SampleObject { Name = "Alice", Age = 30, Email = "old@b.com" };
        var b = new SampleObject { Name = "Bob", Age = 25, Email = "new@b.com" };

        var result = PropertyChangeDetector.HasChanges(a, b, ["Name", "Age", "Email"]);

        Assert.False(result);
    }
}
