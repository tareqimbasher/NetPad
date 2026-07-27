using ElectronSharp.API;
using ElectronSharp.API.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetPad.Apps.UiInterop;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.Scripts;

namespace NetPad.Apps.Shells.Electron.UiInterop;

public class ElectronWindowService(
    WindowManager windowManager,
    ITrivialDataStore trivialDataStore,
    IHostApplicationLifetime applicationLifetime,
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

        // HACK: Electron.App.WindowAllClosed and Electron.App.WillQuit no longer work when we switched to ElectronSharp
        // Stops application when the main window is closed
        window.OnClosed += applicationLifetime.StopApplication;

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
                var isMaximized = await window.IsMaximizedAsync() == true;

                trivialDataStore.Set("main-window.bounds", new WindowState(bounds, isMaximized));
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

        await ShowModalWindowAsync(window, WindowSizes.SettingsHeight, WindowSizes.SettingsWidth);
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

        await ShowModalWindowAsync(window, WindowSizes.ScriptConfigHeight, WindowSizes.ScriptConfigWidth);
    }

    public async Task OpenDataConnectionWindowAsync(Guid? dataConnectionId, bool copy = false, bool isServer = false)
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

        if (isServer)
        {
            queryParams.Add(("is-server", "true"));
        }

        var window = await windowManager.CreateWindowAsync(WindowIds.DataConnection, true, new BrowserWindowOptions
        {
            Title = (dataConnectionId.HasValue ? "Edit" : "New") + (isServer ? " Server" : " Connection"),
            AutoHideMenuBar = true,
            MinWidth = 700,
            MinHeight = 640,
            Show = false
        }, queryParams.ToArray());

        await ShowModalWindowAsync(window, WindowSizes.DataConnectionHeight, WindowSizes.DataConnectionWidth);
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

        await ShowModalWindowAsync(window, WindowSizes.OutputHeight, WindowSizes.OutputWidth);
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

        await ShowModalWindowAsync(window, WindowSizes.CodeHeight, WindowSizes.CodeWidth);
    }

    private async Task ShowModalWindowAsync(BrowserWindow window, double height, double width)
    {
        var mainWindowPosition = await ElectronUtil.MainWindow.GetBoundsAsync();
        var display = await GetMainWindowDisplayAsync(mainWindowPosition);

        window.SetParentWindow(ElectronUtil.MainWindow);

        window.SetPosition(mainWindowPosition.X, mainWindowPosition.Y);

        window.SetBounds(new Rectangle
        {
            X = mainWindowPosition.X,
            Y = mainWindowPosition.Y,
            Height = (int)(display.Bounds.Height * height),
            Width = (int)(display.Bounds.Width * width)
        });

        window.Center();
        window.Show();
    }

    /// <summary>
    /// Shows a window whose size is what its content has to fit rather than a share of the screen.
    /// The size is the page's, not the frame's, and is capped so the window still fits the display
    /// it opens on.
    /// </summary>
    private async Task ShowModalWindowAsync(BrowserWindow window, int height, int width)
    {
        var mainWindowPosition = await ElectronUtil.MainWindow.GetBoundsAsync();
        var display = await GetMainWindowDisplayAsync(mainWindowPosition);

        window.SetParentWindow(ElectronUtil.MainWindow);

        window.SetPosition(mainWindowPosition.X, mainWindowPosition.Y);

        // Size is capped so window always fits the screen.
        window.SetContentSize(
            Math.Min(width, (int)(display.Bounds.Width * 0.92)),
            Math.Min(height, (int)(display.Bounds.Height * 0.92)));

        window.Center();
        window.Show();
    }

    private async Task<Display> GetMainWindowDisplayAsync(Rectangle mainWindowPosition)
    {
        var allDisplays = (await ElectronSharp.API.Electron.Screen.GetAllDisplaysAsync())
            .OrderBy(x => x.Bounds.X)
            .ToArray();

        // The display where most of the main window resides
        var mainWindowMidWayPoint = mainWindowPosition.X + mainWindowPosition.Width / 2;

        return allDisplays.LastOrDefault(x => x.Bounds.X <= mainWindowMidWayPoint) ?? allDisplays[0];
    }
}
