using System.Linq;
using System.Threading.Tasks;
using ElectronNET.API;
using ElectronNET.API.Entities;

namespace NetPad.Utils
{
    public static class ElectronUtil
    {
        private static string? _spaUrl;

        public static void Initialize(string spaUrl)
        {
            _spaUrl = spaUrl;
        }

        public static BrowserWindow? MainWindow => Electron.WindowManager.BrowserWindows.FirstOrDefault();

        public static async Task<BrowserWindow> CreateWindowAsync(
            string windowName,
            BrowserWindowOptions options,
            params (string key, object? value)[] queryParams)
        {
            var url = $"{_spaUrl}?win={windowName}";

            if (queryParams.Any())
            {
                url += "&" + string.Join("&", queryParams.Select(p => $"{p.key}={p.value}"));
            }

            return await Electron.WindowManager.CreateWindowAsync(options, url);
        }
    }
}
