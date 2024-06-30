using ElectronNET.API.Entities;

namespace NetPad.Apps.Shells.Electron.UiInterop;

public record WindowState(Rectangle Bounds, bool IsMaximized)
{
    public bool HasSaneBounds() => Bounds is { Width: >= 100, Height: >= 100 };
}
