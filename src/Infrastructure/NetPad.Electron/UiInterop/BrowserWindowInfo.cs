using ElectronNET.API;

namespace NetPad.Electron.UiInterop;

public class BrowserWindowInfo
{
    public BrowserWindowInfo(
        Guid id,
        string windowName,
        BrowserWindow window,
        bool singleInstance
    )
    {
        Id = id;
        WindowName = windowName;
        Window = window;
        SingleInstance = singleInstance;
    }

    public Guid Id { get; }
    public string WindowName { get; }
    public BrowserWindow Window { get; }
    public bool SingleInstance { get; }
}
