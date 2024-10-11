using ElectronSharp.API;
using ElectronSharp.API.Entities;
using Microsoft.Extensions.Logging;
using NetPad.Apps.UiInterop;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.Scripts;

namespace NetPad.Apps.Shells.Electron.UiInterop;

public class ElectronWindowService(
    WindowManager windowManager,
    ITrivialDataStore trivialDataStore,
    Settings settings,
    ILogger<ElectronWindowService> logger)
    : IUiWindowService
{
    private async Task<Display> PrimaryDisplay() => await ElectronSharp.API.Electron.Screen.GetPrimaryDisplayAsync();

    public async Task OpenMainWindowAsync()
    {
        bool useNativeDecorations = settings.Appearance.Titlebar.Type == TitlebarType.Native;

        var window = await windowManager.CreateWindowAsync(WindowIds.Main, true, new BrowserWindowOptions
        {
            Show = false,
            Frame = useNativeDecorations,
            AutoHideMenuBar = settings.Appearance.Titlebar.MainMenuVisibility == MainMenuVisibility.AutoHidden,
            Fullscreenable = true,
        });

        await RestoreMainWindowPositionAsync(window);
    }

    private async Task RestoreMainWindowPositionAsync(BrowserWindow window)
    {
        try
        {
            var windowState = trivialDataStore.Get<WindowState>("main-window.bounds");

            if (windowState?.HasSaneBounds() == true)
            {
                window.SetBounds(windowState.Bounds);

                if (windowState.IsMaximized)
                {
                    window.Maximize();
                }

                window.Show();
            }
            else
            {
                var display = await PrimaryDisplay();
                window.SetBounds(display.Bounds);
                window.Show();
                window.Maximize();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while restoring main window size and position");
            window.Show();
            window.Maximize();
        }

        window.OnClose += async () =>
        {
            try
            {
                var bounds = await window.GetBoundsAsync();
                var isMaximized = await window.IsMaximizedAsync();

                trivialDataStore.Set("main-window.bounds", new WindowState(bounds, isMaximized ?? false));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while saving window state on close.");
            }
        };
    }

    public async Task OpenSettingsWindowAsync(string? tab = null)
    {
        if (windowManager.FocusExistingWindowIfOpen(WindowIds.Settings))
        {
            return;
        }

        var queryParams = new List<(string, object?)>();
        if (tab != null) queryParams.Add(("tab", tab));

        var window = await windowManager.CreateWindowAsync(WindowIds.Settings, true, new BrowserWindowOptions
        {
            Title = "Settings",
            AutoHideMenuBar = true,
            Show = false
        }, queryParams.ToArray());

        await ShowModalWindowAsync(window, 0.67, 0.5);
    }

    public async Task OpenScriptConfigWindowAsync(Script script, string? tab = null)
    {
        if (windowManager.FocusExistingWindowIfOpen(WindowIds.ScriptConfig))
        {
            return;
        }

        var queryParams = new List<(string, object?)>();
        queryParams.Add(("script-id", script.Id));
        if (tab != null) queryParams.Add(("tab", tab));

        var window = await windowManager.CreateWindowAsync(WindowIds.ScriptConfig, true, new BrowserWindowOptions
        {
            Title = script.Name,
            AutoHideMenuBar = true,
            Show = false
        }, queryParams.ToArray());

        await ShowModalWindowAsync(window, 0.75, 0.8);
    }

    public async Task OpenDataConnectionWindowAsync(Guid? dataConnectionId, bool copy = false)
    {
        if (copy && dataConnectionId == null)
        {
            throw new ArgumentException("Data connection id must be provided when copying a connection.");
        }

        if (windowManager.FocusExistingWindowIfOpen(WindowIds.DataConnection))
        {
            return;
        }

        var queryParams = new List<(string, object?)>();

        if (dataConnectionId != null)
        {
            queryParams.Add(("data-connection-id", dataConnectionId));
        }

        if (copy)
        {
            queryParams.Add(("copy", "true"));
        }

        var window = await windowManager.CreateWindowAsync(WindowIds.DataConnection, true, new BrowserWindowOptions
        {
            Title = (dataConnectionId.HasValue ? "Edit" : "New") + "Connection",
            AutoHideMenuBar = true,
            MinWidth = 550,
            MinHeight = 630,
            Show = false
        }, queryParams.ToArray());

        await ShowModalWindowAsync(window, 0.5, 0.5);
    }

    public async Task OpenOutputWindowAsync()
    {
        if (windowManager.FocusExistingWindowIfOpen(WindowIds.Output))
        {
            return;
        }

        var window = await windowManager.CreateWindowAsync(WindowIds.Output, true, new BrowserWindowOptions
        {
            Title = "Output",
            AutoHideMenuBar = true,
            Show = false
        });

        await ShowModalWindowAsync(window, 0.67, 0.8);
    }

    public async Task OpenCodeWindowAsync()
    {
        if (windowManager.FocusExistingWindowIfOpen(WindowIds.Code))
        {
            return;
        }

        var window = await windowManager.CreateWindowAsync(WindowIds.Code, true, new BrowserWindowOptions
        {
            Title = "Code",
            AutoHideMenuBar = true,
            Show = false
        });

        await ShowModalWindowAsync(window, 0.67, 0.8);
    }

    private async Task ShowModalWindowAsync(BrowserWindow window, double height, double width)
    {
        var mainWindowPosition = await ElectronUtil.MainWindow.GetBoundsAsync();
        var allDisplays = (await ElectronSharp.API.Electron.Screen.GetAllDisplaysAsync())
            .OrderBy(x => x.Bounds.X)
            .ToArray();

        // Find display where most of main window resides
        var mainWindowMidWayPoint = mainWindowPosition.X + mainWindowPosition.Width / 2;
        var mainWindowDisplay = allDisplays.LastOrDefault(x => x.Bounds.X <= mainWindowMidWayPoint)
                                ?? allDisplays[0];

        window.SetParentWindow(ElectronUtil.MainWindow);

        window.SetPosition(mainWindowPosition.X, mainWindowPosition.Y);

        window.SetBounds(new Rectangle
        {
            X = mainWindowPosition.X,
            Y = mainWindowPosition.Y,
            Height = (int)(mainWindowDisplay.Bounds.Height * height),
            Width = (int)(mainWindowDisplay.Bounds.Width * width)
        });

        window.Center();
        window.Show();
    }
}
