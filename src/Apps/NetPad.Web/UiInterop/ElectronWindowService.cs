using System.Linq;
using System.Threading.Tasks;
using ElectronNET.API;
using ElectronNET.API.Entities;
using NetPad.Scripts;
using NetPad.Services;

namespace NetPad.UiInterop
{
    public class ElectronWindowService : IUiWindowService
    {
        private readonly string _hostUrl;

        public ElectronWindowService(HostInfo hostInfo)
        {
            _hostUrl = hostInfo.HostUrl;
        }

        private async Task<Display> PrimaryDisplay() => await Electron.Screen.GetPrimaryDisplayAsync();

        public async Task OpenMainWindowAsync()
        {
            var display = await PrimaryDisplay();
            await CreateWindowAsync("main", new BrowserWindowOptions
            {
                Height = display.Bounds.Height * 2 / 3,
                Width = display.Bounds.Width * 2 / 3,
            });
        }

        public async Task OpenScriptConfigWindowAsync(Script script)
        {
            var display = await PrimaryDisplay();
            var window = await CreateWindowAsync("script-config", new BrowserWindowOptions
            {
                Title = script.Name,
                Height = display.Bounds.Height * 3 / 5,
                Width = display.Bounds.Width * 3 / 5,
                AutoHideMenuBar = true,
            }, ("script-id", script.Id));

            window.SetParentWindow(ElectronUtil.MainWindow);
            var mainWindowPosition = await ElectronUtil.MainWindow.GetPositionAsync();
            window.SetPosition(mainWindowPosition[0], mainWindowPosition[1]);
            window.Center();
        }

        private async Task<BrowserWindow> CreateWindowAsync(
            string windowName,
            BrowserWindowOptions options,
            params (string key, object? value)[] queryParams)
        {
            var url = $"{_hostUrl}?win={windowName}";

            if (queryParams.Any())
            {
                url += "&" + string.Join("&", queryParams.Select(p => $"{p.key}={p.value}"));
            }

            options.MinHeight = 100;
            options.MinWidth = 100;
            options.Center = true;

            return await Electron.WindowManager.CreateWindowAsync(options, url);
        }
    }
}
