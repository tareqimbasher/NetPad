using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.Extensions.Logging;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.Scripts;
using NetPad.UiInterop;

namespace NetPad.Electron.UiInterop;

public class ElectronWindowService : IUiWindowService
{
    private readonly WindowManager _windowManager;
    private readonly ITrivialDataStore _trivialDataStore;
    private readonly Settings _settings;
    private readonly ILogger<ElectronWindowService> _logger;

    public ElectronWindowService(
        WindowManager windowManager,
        ITrivialDataStore trivialDataStore,
        Settings settings,
        ILogger<ElectronWindowService> logger)
    {
        _windowManager = windowManager;
        _trivialDataStore = trivialDataStore;
        _settings = settings;
        _logger = logger;
    }

    private async Task<Display> PrimaryDisplay() => await ElectronNET.API.Electron.Screen.GetPrimaryDisplayAsync();

    public async Task OpenMainWindowAsync()
    {
        bool useNativeDecorations = _settings.Appearance.Titlebar.Type == TitlebarType.Native;

        var window = await _windowManager.CreateWindowAsync("main", true, new BrowserWindowOptions
        {
            Show = false,
            Frame = useNativeDecorations,
            AutoHideMenuBar = _settings.Appearance.Titlebar.MainMenuVisibility == MainMenuVisibility.AutoHidden,
            Fullscreenable = true,
        });

        await RestoreMainWindowPositionAsync(window);
    }

    private async Task RestoreMainWindowPositionAsync(BrowserWindow window)
    {
        try
        {
            var windowState = _trivialDataStore.Get<WindowState>("main-window.bounds");

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
            _logger.LogError(ex, "Error while restoring main window size and position");
            window.Show();
            window.Maximize();
        }

        window.OnClose += async () =>
        {
            _trivialDataStore.Set("main-window.bounds",
                new WindowState(await window.GetBoundsAsync(), await window.IsMaximizedAsync()));
        };
    }

    public async Task OpenSettingsWindowAsync(string? tab = null)
    {
        const string windowName = "settings";

        if (_windowManager.FocusExistingWindowIfOpen(windowName))
        {
            return;
        }

        var queryParams = new List<(string, object?)>();
        if (tab != null) queryParams.Add(("tab", tab));

        var window = await _windowManager.CreateWindowAsync(windowName, true, new BrowserWindowOptions
        {
            Title = "Settings",
            AutoHideMenuBar = true,
            Show = false
        }, queryParams.ToArray());

        await ShowModalWindowAsync(window, 0.67, 0.5);
    }

    public async Task OpenScriptConfigWindowAsync(Script script, string? tab = null)
    {
        const string windowName = "script-config";

        if (_windowManager.FocusExistingWindowIfOpen(windowName))
        {
            return;
        }

        var queryParams = new List<(string, object?)>();
        queryParams.Add(("script-id", script.Id));
        if (tab != null) queryParams.Add(("tab", tab));

        var window = await _windowManager.CreateWindowAsync(windowName, true, new BrowserWindowOptions
        {
            Title = script.Name,
            AutoHideMenuBar = true,
            Show = false
        }, queryParams.ToArray());

        await ShowModalWindowAsync(window, 0.67, 0.8);
    }

    public async Task OpenDataConnectionWindowAsync(Guid? dataConnectionId)
    {
        const string windowName = "data-connection";

        if (_windowManager.FocusExistingWindowIfOpen(windowName))
        {
            return;
        }

        var queryParams = new List<(string, object?)>();
        if (dataConnectionId != null) queryParams.Add(("data-connection-id", dataConnectionId));

        var window = await _windowManager.CreateWindowAsync(windowName, true, new BrowserWindowOptions
        {
            Title = (dataConnectionId.HasValue ? "Edit" : "New") + "Connection",
            AutoHideMenuBar = true,
            MinWidth = 550,
            MinHeight = 550,
            Show = false
        }, queryParams.ToArray());

        await ShowModalWindowAsync(window, 0.4, 0.5);
    }

    public async Task OpenOutputWindowAsync()
    {
        const string windowName = "output";

        if (_windowManager.FocusExistingWindowIfOpen(windowName))
        {
            return;
        }

        var window = await _windowManager.CreateWindowAsync(windowName, true, new BrowserWindowOptions
        {
            Title = "Output",
            AutoHideMenuBar = true,
            Show = false
        });

        await ShowModalWindowAsync(window, 0.67, 0.8);
    }

    private async Task ShowModalWindowAsync(BrowserWindow window, double height, double width)
    {
        var mainWindowPosition = await ElectronUtil.MainWindow.GetBoundsAsync();
        var allDisplays = (await ElectronNET.API.Electron.Screen.GetAllDisplaysAsync())
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
