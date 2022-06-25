using System.Runtime.InteropServices;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.Extensions.Hosting;
using NetPad.Scripts;
using NetPad.UiInterop;

namespace NetPad.Electron.UiInterop
{
    public class ElectronWindowService : IUiWindowService
    {
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly string _hostUrl;

        public ElectronWindowService(HostInfo hostInfo, IHostApplicationLifetime applicationLifetime)
        {
            _applicationLifetime = applicationLifetime;
            _hostUrl = hostInfo.HostUrl;
        }

        private async Task<Display> PrimaryDisplay() => await ElectronNET.API.Electron.Screen.GetPrimaryDisplayAsync();

        public async Task OpenMainWindowAsync()
        {
            var display = await PrimaryDisplay();
            var window = await CreateWindowAsync("main", new BrowserWindowOptions
            {
                Height = display.Bounds.Height * 2 / 3,
                Width = display.Bounds.Width * 2 / 3,
                X = display.Bounds.X,
                Y = display.Bounds.Y
            });

            window.Center();
            window.Maximize();

            ElectronNET.API.Electron.App.WindowAllClosed += () =>
            {
                // On macOS it is common for applications and their menu bar
                // to stay active until the user quits explicitly with Cmd + Q
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    ElectronNET.API.Electron.App.Quit();
                }
            };

            ElectronNET.API.Electron.App.WillQuit += (args) =>
            {
                _applicationLifetime.StopApplication();
                return Task.CompletedTask;
            };
        }

        public async Task OpenSettingsWindowAsync()
        {
            var display = await PrimaryDisplay();
            var window = await CreateWindowAsync("settings", new BrowserWindowOptions
            {
                Title = "Settings",
                Height = display.Bounds.Height * 1 / 2,
                Width = display.Bounds.Width * 1 / 2,
                AutoHideMenuBar = true,
            });

            window.SetParentWindow(ElectronUtil.MainWindow);
            var mainWindowPosition = await ElectronUtil.MainWindow.GetPositionAsync();
            window.SetPosition(mainWindowPosition[0], mainWindowPosition[1]);
            window.Center();
        }

        public async Task OpenScriptConfigWindowAsync(Script script)
        {
            var display = await PrimaryDisplay();
            var window = await CreateWindowAsync("script-config", new BrowserWindowOptions
            {
                Title = script.Name,
                Height = display.Bounds.Height * 2 / 3,
                Width = display.Bounds.Width * 4 / 5,
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

            return await ElectronNET.API.Electron.WindowManager.CreateWindowAsync(options, url);
        }
    }
}
