using System.Text.Json;
using NetPad.Apps.UiInterop;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.Apps.Common.Tests.UiInterop;

public class WindowDeepLinkRequestedEventTests
{
    [Fact]
    public void NullParamValues_AreSerialized()
    {
        // A retarget clears a param by sending it as null. If the serializer dropped null entries,
        // the window would keep the old value. SignalR sends events with these same options.
        var @event = new WindowDeepLinkRequestedEvent(WindowIds.ScriptConfig, new Dictionary<string, string?>
        {
            ["script-id"] = Guid.NewGuid().ToString(),
            ["tab"] = null,
        });

        var json = JsonSerializer.Serialize(@event);

        var tab = JsonDocument.Parse(json).RootElement.GetProperty("params").GetProperty("tab");
        Assert.Equal(JsonValueKind.Null, tab.ValueKind);
    }
}
