using NetPad.Apps.Common.Tests.Helpers;
using NetPad.Apps.Services;
using NetPad.Events;
using NetPad.Utilities;

namespace NetPad.Apps.Common.Tests.Services;

public class RecentScriptsServiceTests
{
    [Fact]
    public void Add_Inserts_Path_At_Front()
    {
        var (svc, _, _) = CreateService();

        svc.Add("/a/one.netpad");
        svc.Add("/a/two.netpad");

        Assert.Equal(["/a/two.netpad", "/a/one.netpad"], svc.Get());
    }

    [Fact]
    public void Add_Dedups_Same_Path_Regardless_Of_Casing_On_Case_Insensitive_Platforms()
    {
        // Linux/FreeBSD filesystems are case-sensitive so "/a/foo" and "/A/FOO" are different files there;
        // skip this assertion on those platforms (the inverse is checked in the sibling test).
        if (PlatformUtil.PathComparison != StringComparison.OrdinalIgnoreCase) return;

        var (svc, _, _) = CreateService();

        svc.Add("/a/foo.netpad");
        svc.Add("/a/bar.netpad");
        svc.Add("/A/FOO.NETPAD");

        Assert.Equal(["/A/FOO.NETPAD", "/a/bar.netpad"], svc.Get());
    }

    [Fact]
    public void Add_Keeps_Same_Path_With_Different_Casing_As_Separate_On_Case_Sensitive_Platforms()
    {
        if (PlatformUtil.PathComparison != StringComparison.Ordinal) return;

        var (svc, _, _) = CreateService();

        svc.Add("/a/foo.netpad");
        svc.Add("/A/FOO.NETPAD");

        Assert.Equal(["/A/FOO.NETPAD", "/a/foo.netpad"], svc.Get());
    }

    [Fact]
    public void Add_Always_Dedups_Identical_Paths()
    {
        var (svc, _, _) = CreateService();

        svc.Add("/a/foo.netpad");
        svc.Add("/a/bar.netpad");
        svc.Add("/a/foo.netpad");

        Assert.Equal(["/a/foo.netpad", "/a/bar.netpad"], svc.Get());
    }

    [Fact]
    public void Add_Caps_List_At_MaxEntries()
    {
        var (svc, _, _) = CreateService();

        for (int i = 0; i < RecentScriptsService.MaxEntries + 5; i++)
        {
            svc.Add($"/a/file{i}.netpad");
        }

        Assert.Equal(RecentScriptsService.MaxEntries, svc.Get().Count);
        Assert.Equal($"/a/file{RecentScriptsService.MaxEntries + 4}.netpad", svc.Get()[0]);
    }

    [Fact]
    public void Add_Normalizes_Backslashes_To_Forward_Slashes()
    {
        var (svc, _, _) = CreateService();

        svc.Add("C:\\folder\\file.netpad");

        Assert.Equal("C:/folder/file.netpad", svc.Get()[0]);
    }

    [Fact]
    public void Clear_Empties_The_List()
    {
        var (svc, _, _) = CreateService();
        svc.Add("/a/one.netpad");
        svc.Add("/a/two.netpad");

        svc.Clear();

        Assert.Empty(svc.Get());
    }

    [Fact]
    public async Task Add_Publishes_RecentScriptsChangedEvent()
    {
        var (svc, _, captured) = CreateService();

        svc.Add("/a/one.netpad");

        await WaitFor(() => captured.Count >= 1);
        var ev = Assert.Single(captured);
        Assert.Equal(["/a/one.netpad"], ev.RecentScripts);
    }

    [Fact]
    public async Task Clear_Publishes_Empty_RecentScriptsChangedEvent()
    {
        var (svc, _, captured) = CreateService();
        svc.Add("/a/one.netpad");
        await WaitFor(() => captured.Count >= 1);
        captured.Clear();

        svc.Clear();

        await WaitFor(() => captured.Count >= 1);
        var ev = Assert.Single(captured);
        Assert.Empty(ev.RecentScripts);
    }

    [Fact]
    public void Add_Skips_Null_Or_Whitespace_Paths()
    {
        var (svc, _, _) = CreateService();

        svc.Add("");
        svc.Add("   ");

        Assert.Empty(svc.Get());
    }

    private static (RecentScriptsService svc, EventBus eventBus, List<RecentScriptsChangedEvent> capturedEvents)
        CreateService()
    {
        var store = new InMemoryTrivialDataStore();
        var eventBus = new EventBus();
        var captured = new List<RecentScriptsChangedEvent>();
        eventBus.Subscribe<RecentScriptsChangedEvent>(ev =>
        {
            lock (captured)
            {
                captured.Add(ev);
            }
            return Task.CompletedTask;
        }, useStrongReferences: true);

        var svc = new RecentScriptsService(store, eventBus);
        return (svc, eventBus, captured);
    }

    private static async Task WaitFor(Func<bool> condition, int timeoutMs = 2000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (condition()) return;
            await Task.Delay(10);
        }

        if (!condition())
            throw new TimeoutException("Condition not met within timeout.");
    }
}
