using Microsoft.AspNetCore.DataProtection;
using NetPad.IO;
using NetPad.IO.Stores;

namespace NetPad.Runtime.Tests.IO.Stores;

public class JsonKeyValueStoreTests : IDisposable
{
    private readonly string _tempDir;

    public JsonKeyValueStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "NetPad_Tests", nameof(JsonKeyValueStoreTests),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private JsonKeyValueStore<TestStore, TestItem> CreateStore(string fileName = "store.json")
    {
        var filePath = new FilePath(Path.Combine(_tempDir, fileName));
        return new JsonKeyValueStore<TestStore, TestItem>(filePath, new FakeDataProtector());
    }

    // --- List ---

    [Fact]
    public void List_ReturnsEmpty_WhenStoreIsNew()
    {
        var store = CreateStore();

        var items = store.List();

        Assert.Empty(items);
    }

    [Fact]
    public void List_ReturnsAllItems()
    {
        var store = CreateStore();
        store.Save("key1", "value1");
        store.Save("key2", "value2");

        var items = store.List();

        Assert.Equal(2, items.Length);
    }

    // --- Save / Get ---

    [Fact]
    public void Save_And_Get_RoundTripsValue()
    {
        var store = CreateStore();

        store.Save("mykey", "hello world");
        var item = store.Get("mykey");

        Assert.NotNull(item);
        Assert.Equal("mykey", item!.Key);
        Assert.NotNull(item.Value);
    }

    [Fact]
    public void Save_OverwritesExistingValue()
    {
        var store = CreateStore();
        store.Save("key", "first");
        store.Save("key", "second");

        var items = store.List();

        Assert.Single(items);
    }

    [Fact]
    public void Save_SetsUpdatedAtUtc()
    {
        var store = CreateStore();
        var before = DateTime.UtcNow;

        store.Save("key", "value");

        var item = store.Get("key");
        Assert.NotNull(item);
        Assert.True(item!.UpdatedAtUtc >= before);
        Assert.True(item.UpdatedAtUtc <= DateTime.UtcNow);
    }

    [Fact]
    public void Save_ThrowsForEmptyKey()
    {
        var store = CreateStore();

        Assert.Throws<ArgumentException>(() => store.Save("", "value"));
    }

    [Fact]
    public void Save_ThrowsForWhitespaceKey()
    {
        var store = CreateStore();

        Assert.Throws<ArgumentException>(() => store.Save("   ", "value"));
    }

    [Fact]
    public void Save_NullValue_SetsItemValueToNull()
    {
        var store = CreateStore();

        store.Save<string?>("key", null);
        var item = store.Get("key");

        Assert.NotNull(item);
        Assert.Null(item!.Value);
    }

    [Fact]
    public void Get_ReturnsNull_WhenKeyDoesNotExist()
    {
        var store = CreateStore();

        var item = store.Get("nonexistent");

        Assert.Null(item);
    }

    // --- GetValue<T> ---

    [Fact]
    public void GetValue_DeserializesToCorrectType()
    {
        var store = CreateStore();
        store.Save("key", new TestPayload { Name = "test", Count = 42 });

        var value = store.GetValue<TestPayload>("key");

        Assert.NotNull(value);
        Assert.Equal("test", value!.Name);
        Assert.Equal(42, value.Count);
    }

    [Fact]
    public void GetValue_ThrowsForNonexistentKey()
    {
        var store = CreateStore();

        Assert.Throws<KeyNotFoundException>(() => store.GetValue<string>("nonexistent"));
    }

    // --- GetUnprotectedRawStringValue ---

    [Fact]
    public void GetUnprotectedRawStringValue_ThrowsForNonexistentKey()
    {
        var store = CreateStore();

        Assert.Throws<KeyNotFoundException>(() => store.GetUnprotectedRawStringValue("nope"));
    }

    [Fact]
    public void GetUnprotectedRawStringValue_ReturnsNull_WhenValueIsNull()
    {
        var store = CreateStore();
        store.Save<string?>("key", null);

        var result = store.GetUnprotectedRawStringValue("key");

        Assert.Null(result);
    }

    // --- Delete ---

    [Fact]
    public void Delete_ByKey_RemovesItem()
    {
        var store = CreateStore();
        store.Save("key", "value");

        var deleted = store.Delete("key");

        Assert.True(deleted);
        Assert.Null(store.Get("key"));
    }

    [Fact]
    public void Delete_ByKey_ReturnsFalse_WhenKeyDoesNotExist()
    {
        var store = CreateStore();

        var deleted = store.Delete("nonexistent");

        Assert.False(deleted);
    }

    [Fact]
    public void Delete_ByPredicate_RemovesMatchingItems()
    {
        var store = CreateStore();
        store.Save("keep", "value1");
        store.Save("remove1", "value2");
        store.Save("remove2", "value3");

        store.Delete(item => item.Key.StartsWith("remove"));

        Assert.Single(store.List());
        Assert.NotNull(store.Get("keep"));
    }

    [Fact]
    public void Delete_ByPredicate_DoesNothing_WhenNoMatch()
    {
        var store = CreateStore();
        store.Save("key", "value");

        store.Delete(item => item.Key == "nonexistent");

        Assert.Single(store.List());
    }

    // --- Persistence ---

    [Fact]
    public void Data_PersistsAcrossInstances()
    {
        var fileName = "persist.json";
        var store1 = CreateStore(fileName);
        store1.Save("key", "persisted");

        var store2 = CreateStore(fileName);
        var item = store2.Get("key");

        Assert.NotNull(item);
    }

    [Fact]
    public void Store_CreatesFileOnFirstAccess()
    {
        var fileName = "newfile.json";
        var filePath = Path.Combine(_tempDir, fileName);
        Assert.False(File.Exists(filePath));

        var store = CreateStore(fileName);
        store.List();

        Assert.True(File.Exists(filePath));
    }

    // --- Protection (with IProtectableItem) ---

    [Fact]
    public void Save_WithProtect_ProtectsValue()
    {
        var filePath = new FilePath(Path.Combine(_tempDir, "protected.json"));
        var protector = new FakeDataProtector();
        var store = new JsonKeyValueStore<ProtectedStore, ProtectedItem>(filePath, protector);

        store.Save("secret", "my-password", protect: true);
        var item = store.Get("secret");

        Assert.NotNull(item);
        Assert.True(item!.IsProtected);
        // The stored value should be the "protected" version
        Assert.NotEqual("\"my-password\"", item.Value);
    }

    [Fact]
    public void GetUnprotectedRawStringValue_UnprotectsProtectedValue()
    {
        var filePath = new FilePath(Path.Combine(_tempDir, "protected2.json"));
        var protector = new FakeDataProtector();
        var store = new JsonKeyValueStore<ProtectedStore, ProtectedItem>(filePath, protector);

        store.Save("secret", "my-password", protect: true);
        var raw = store.GetUnprotectedRawStringValue("secret");

        // FakeDataProtector reverses the string, then Unprotect reverses back
        Assert.Equal("\"my-password\"", raw);
    }

    // --- Test types ---

    private class TestPayload
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    private class TestStore : IKeyedItemsStore<TestItem>
    {
        public Dictionary<string, TestItem> Items { get; set; } = new();
    }

    private class TestItem : IKeyedItem
    {
        public string Key { get; init; } = "";
        public string? Value { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }

    private class ProtectedStore : IKeyedItemsStore<ProtectedItem>
    {
        public Dictionary<string, ProtectedItem> Items { get; set; } = new();
    }

    private class ProtectedItem : IProtectableItem
    {
        public string Key { get; init; } = "";
        public string? Value { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public bool IsProtected { get; set; }
    }

    /// <summary>
    /// A fake IDataProtector that reverses the string for "protection".
    /// Protect(Unprotect(x)) == x.
    /// </summary>
    private class FakeDataProtector : IDataProtector
    {
        public IDataProtector CreateProtector(string purpose) => this;

        public byte[] Protect(byte[] plaintext)
        {
            var reversed = plaintext.Reverse().ToArray();
            return reversed;
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            var reversed = protectedData.Reverse().ToArray();
            return reversed;
        }
    }
}
