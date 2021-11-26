using System.Linq;
using ElectronNET.API;

namespace NetPad.Services
{
    public static class ElectronUtil
    {
        public static BrowserWindow MainWindow => Electron.WindowManager.BrowserWindows.First();
    }
}
