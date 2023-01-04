using ElectronNET.API;

namespace NetPad.Electron.UiInterop;

public static class ElectronUtil
{
    public static BrowserWindow MainWindow => ElectronNET.API.Electron.WindowManager.BrowserWindows.First();
}
