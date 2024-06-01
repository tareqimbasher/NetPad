using ElectronNET.API;

namespace NetPad.Apps.Shells.Electron.UiInterop;

public class BrowserWindowInfo(
    Guid id,
    string windowName,
    BrowserWindow window,
    bool singleInstance)
{
    public Guid Id { get; } = id;
    public string WindowName { get; } = windowName;
    public BrowserWindow Window { get; } = window;
    public bool SingleInstance { get; } = singleInstance;
}
