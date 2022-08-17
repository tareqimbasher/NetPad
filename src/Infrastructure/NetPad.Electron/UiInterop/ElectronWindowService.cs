using ElectronNET.API;
using ElectronNET.API.Entities;
using NetPad.Scripts;
using NetPad.UiInterop;

namespace NetPad.Electron.UiInterop
{
    public class ElectronWindowService : IUiWindowService
    {
        private readonly HostInfo _hostInfo;
        private static readonly Dictionary<string, BrowserWindow> _singleInstanceWindows = new();

        public ElectronWindowService(HostInfo hostInfo)
        {
            _hostInfo = hostInfo;
        }

        private async Task<Display> PrimaryDisplay() => await ElectronNET.API.Electron.Screen.GetPrimaryDisplayAsync();

        public async Task OpenMainWindowAsync()
        {
            var display = await PrimaryDisplay();
            var window = await CreateWindowAsync("main", false, new BrowserWindowOptions
            {
                Height = display.Bounds.Height * 2 / 3,
                Width = display.Bounds.Width * 2 / 3,
                X = display.Bounds.X,
                Y = display.Bounds.Y,
                AutoHideMenuBar = true,
            });

            window.Center();
            window.Maximize();
        }

        public async Task OpenSettingsWindowAsync(string? tab = null)
        {
            const string windowName = "settings";

            if (FocusExistingWindowIfOpen(windowName))
            {
                return;
            }

            var display = await PrimaryDisplay();
            var window = await CreateWindowAsync(windowName, true, new BrowserWindowOptions
            {
                Title = "Settings",
                Height = display.Bounds.Height * 2 / 3,
                Width = display.Bounds.Width * 1 / 2,
                AutoHideMenuBar = true,
            }, ("tab", tab));

            window.SetParentWindow(ElectronUtil.MainWindow);
            var mainWindowPosition = await ElectronUtil.MainWindow.GetPositionAsync();
            window.SetPosition(mainWindowPosition[0], mainWindowPosition[1]);
            window.Center();
        }

        public async Task OpenScriptConfigWindowAsync(Script script, string? tab = null)
        {
            const string windowName = "script-config";

            if (FocusExistingWindowIfOpen(windowName))
            {
                return;
            }

            var display = await PrimaryDisplay();
            var window = await CreateWindowAsync(windowName, true, new BrowserWindowOptions
            {
                Title = script.Name,
                Height = display.Bounds.Height * 2 / 3,
                Width = display.Bounds.Width * 4 / 5,
                AutoHideMenuBar = true,
            }, ("script-id", script.Id), ("tab", tab));

            window.SetParentWindow(ElectronUtil.MainWindow);
            var mainWindowPosition = await ElectronUtil.MainWindow.GetPositionAsync();
            window.SetPosition(mainWindowPosition[0], mainWindowPosition[1]);
            window.Center();
        }

        public async Task OpenDataConnectionWindowAsync(Guid? dataConnectionId)
        {
            const string windowName = "data-connection";

            if (FocusExistingWindowIfOpen(windowName))
            {
                return;
            }

            var display = await PrimaryDisplay();
            var window = await CreateWindowAsync(windowName, true, new BrowserWindowOptions
            {
                Title = (dataConnectionId.HasValue ? "Edit" : "New") + "Connection",
                Height = display.Bounds.Height * 4 / 10,
                Width = 550,
                AutoHideMenuBar = true,
                MinWidth = 550,
                MinHeight = 550
            }, ("data-connection-id", dataConnectionId));

            window.SetParentWindow(ElectronUtil.MainWindow);
            var mainWindowPosition = await ElectronUtil.MainWindow.GetPositionAsync();
            window.SetPosition(mainWindowPosition[0], mainWindowPosition[1]);
            window.Center();
        }

        private async Task<BrowserWindow> CreateWindowAsync(
            string windowName,
            bool singleInstance,
            BrowserWindowOptions options,
            params (string key, object? value)[] queryParams)
        {
            var url = $"{_hostInfo.HostUrl}?win={windowName}";

            if (queryParams.Any())
            {
                url += "&" + string.Join("&", queryParams.Select(p => $"{p.key}={p.value}"));
            }

            if (options.MinHeight == 0) options.MinHeight = 100;
            if (options.MinWidth == 0) options.MinWidth = 100;
            options.Center = true;

            var window = await ElectronNET.API.Electron.WindowManager.CreateWindowAsync(options, url);

            if (singleInstance)
            {
                _singleInstanceWindows.Add(windowName, window);
                window.OnClosed += () => _singleInstanceWindows.Remove(windowName);
            }

            return window;
        }

        private bool FocusExistingWindowIfOpen(string windowName)
        {
            if (_singleInstanceWindows.TryGetValue(windowName, out var window))
            {
                window.Focus();
                return true;
            }

            return false;
        }
    }
}
