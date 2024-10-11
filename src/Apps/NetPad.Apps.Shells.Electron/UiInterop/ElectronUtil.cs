using ElectronSharp.API;

namespace NetPad.Apps.Shells.Electron.UiInterop;

public static class ElectronUtil
{
    public static BrowserWindow MainWindow => ElectronSharp.API.Electron.WindowManager.BrowserWindows.First();
}
