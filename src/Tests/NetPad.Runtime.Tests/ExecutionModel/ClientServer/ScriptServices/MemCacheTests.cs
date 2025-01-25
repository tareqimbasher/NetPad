using NetPad.ExecutionModel.ClientServer.ScriptServices;
using Xunit;

namespace NetPad.Runtime.Tests.ExecutionModel.ClientServer.ScriptServices;

public class MemCacheTests
{
    [Fact]
    public void ContainsKeyReturnsFalseWhenKeyDoesNotExist()
    {
        var memCache = new MemCache();
        Assert.False(memCache.ContainsKey("key1"));
    }

    [Fact]
    public void ContainsKeyReturnsTrueWhenKeyExists()
    {
        var memCache = new MemCache();

        memCache.Set("key1", 1);

        Assert.True(memCache.ContainsKey("key1"));
    }

    [Fact]
    public void Get()
    {
        var memCache = new MemCache();
        memCache.Set("key1", 1);
        Assert.Equal(1, memCache.Get<int>("key1"));
    }

    [Fact]
    public void TryGetReturnsTrueWhenKeyExists()
    {
        var memCache = new MemCache();

        memCache.Set("key1", 1);

        Assert.True(memCache.TryGet<int>("key1", out var value));

        Assert.Equal(1, value);
    }

    [Fact]
    public void TryGetReturnsFalseWhenKeyExists()
    {
        var memCache = new MemCache();

        memCache.Set("key1", 1);

        Assert.False(memCache.TryGet<int>("key2", out var value));
        Assert.Equal(default, value);
    }

    [Fact]
    public void SetInstance()
    {
        var memCache = new MemCache();
        var person = new Person();

        memCache.Set("key1", person);

        Assert.Equal(person, memCache.Get<Person>("key1"));
    }

    [Fact]
    public void SetInstanceStoresInstanceInstantly()
    {
        var memCache = new MemCache();

        memCache.Set("key1", DateTime.Now);
        Thread.Sleep(1000);

        Assert.InRange((DateTime.Now - memCache.Get<DateTime>("key1")).TotalMilliseconds, 900, 1300);
    }

    [Fact]
    public void SetFactoryExecutesOnFirstCall()
    {
        var memCache = new MemCache();

        memCache.Set("key1", () => DateTime.Now);
        Thread.Sleep(1000);

        Assert.True((memCache.Get<DateTime>("key1") - DateTime.Now).TotalMilliseconds < 500);
    }

    [Fact]
    public void SetFactoryExecutesOnFirstCallAndThenCachesValue()
    {
        var memCache = new MemCache();

        memCache.Set("key1", () => DateTime.Now);
        Thread.Sleep(1000);

        var cached = memCache.Get<DateTime>("key1");
        Assert.True((cached - DateTime.Now).TotalMilliseconds < 500);
        Thread.Sleep(100);
        Assert.Equal(cached, memCache.Get<DateTime>("key1"));
    }

    [Fact]
    public void SetAsyncFactoryExecutesOnFirstCall()
    {
        var memCache = new MemCache();

        memCache.Set("key1", async () => await Task.Run(() => DateTime.Now));
        Thread.Sleep(1000);

        Assert.True((memCache.Get<DateTime>("key1") - DateTime.Now).TotalMilliseconds < 500);
    }

    [Fact]
    public void SetAsyncFactoryExecutesOnFirstCallAndThenCachesValue()
    {
        var memCache = new MemCache();

        memCache.Set("key1", async () => await Task.Run(() => DateTime.Now));
        Thread.Sleep(1000);

        var cached = memCache.Get<DateTime>("key1");
        Assert.True((cached - DateTime.Now).TotalMilliseconds < 500);
        Thread.Sleep(100);
        Assert.Equal(cached, memCache.Get<DateTime>("key1"));
    }

    [Fact]
    public void Remove()
    {
        var memCache = new MemCache();
        memCache.Set("key1", 1);
        Assert.True(memCache.ContainsKey("key1"));

        memCache.Remove("key1");

        Assert.False(memCache.ContainsKey("key1"));
    }

    [Fact]
    public void Clear()
    {
        var memCache = new MemCache();
        memCache.Set("key1", 1);
        Assert.True(memCache.ContainsKey("key1"));

        memCache.Clear();

        Assert.False(memCache.ContainsKey("key1"));
    }

    record Person;
}
