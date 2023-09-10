using ElectronNET.API.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using NetPad.CQs;
using NetPad.Scripts;
using NetPad.Sessions;
using NetPad.UiInterop;

namespace NetPad.Electron.UiInterop;

public class ElectronWindowService : IUiWindowService
{
    private static bool _isMenuInitialized;
    private readonly WindowManager _windowManager;
    private readonly ISession _session;
    private readonly IMediator _mediator;
    private readonly ILogger<ElectronWindowService> _logger;

    public ElectronWindowService(
        WindowManager windowManager,
        ISession session,
        IMediator mediator,
        ILogger<ElectronWindowService> logger)
    {
        _windowManager = windowManager;
        _session = session;
        _mediator = mediator;
        _logger = logger;
    }

    private async Task<Display> PrimaryDisplay() => await ElectronNET.API.Electron.Screen.GetPrimaryDisplayAsync();

    public async Task<WindowState?> GetWindowStateAsync()
    {
        try
        {
            var main = ElectronUtil.MainWindow;

            WindowViewStatus viewStatus;

            if (await main.IsMaximizedAsync())
                viewStatus = WindowViewStatus.Maximized;
            else if (await main.IsMinimizedAsync())
                viewStatus = WindowViewStatus.Minimized;
            else
                viewStatus = WindowViewStatus.UnMaximized;

            bool isAlwaysOnTop = await main.IsAlwaysOnTopAsync();
            return new WindowState(viewStatus, isAlwaysOnTop);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting window state");
            return new WindowState(WindowViewStatus.Unknown, false);
        }
    }

    public async Task MaximizeMainWindowAsync()
    {
        try
        {
            var window = ElectronUtil.MainWindow;

            if (await window.IsMaximizedAsync())
                window.Unmaximize();
            else
                window.Maximize();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error maximizing window");
        }
    }

    public async Task MinimizeMainWindowAsync()
    {
        try
        {
            var window = ElectronUtil.MainWindow;

            if (await window.IsMinimizedAsync())
                window.Restore();
            else
                window.Minimize();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error minimizing window");
        }
    }

    public async Task ToggleFullScreenAsync()
    {
        try
        {
            var window = ElectronUtil.MainWindow;

            if (await window.IsFullScreenAsync())
                window.SetFullScreen(false);
            else
                window.SetFullScreen(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling full screen mode");
        }
    }

    public async Task ToggleAlwaysOnTopMainWindowAsync()
    {
        try
        {
            var window = ElectronUtil.MainWindow;

            if (await window.IsAlwaysOnTopAsync())
                window.SetAlwaysOnTop(false);
            else
                window.SetAlwaysOnTop(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling always on top");
        }
    }


    public async Task OpenMainWindowAsync()
    {
        var display = await PrimaryDisplay();
        var window = await _windowManager.CreateWindowAsync("main", true, new BrowserWindowOptions
        {
            Show = false,
            Height = display.Bounds.Height * 2 / 3,
            Width = display.Bounds.Width * 2 / 3,
            X = display.Bounds.X,
            Y = display.Bounds.Y,
            Frame = false,
            Fullscreenable = true,
        });

        window.Show();
        window.Maximize();

        if (!_isMenuInitialized)
        {
            InitializeMenu();
            _isMenuInitialized = true;
        }
    }

    public async Task OpenSettingsWindowAsync(string? tab = null)
    {
        const string windowName = "settings";

        if (_windowManager.FocusExistingWindowIfOpen(windowName))
        {
            return;
        }

        var display = await PrimaryDisplay();

        var queryParams = new List<(string, object?)>();
        if (tab != null) queryParams.Add(("tab", tab));

        var window = await _windowManager.CreateWindowAsync(windowName, true, new BrowserWindowOptions
        {
            Title = "Settings",
            Height = display.Bounds.Height * 2 / 3,
            Width = display.Bounds.Width * 1 / 2,
            AutoHideMenuBar = true
        }, queryParams.ToArray());

        window.SetParentWindow(ElectronUtil.MainWindow);
        var mainWindowPosition = await ElectronUtil.MainWindow.GetPositionAsync();
        window.SetPosition(mainWindowPosition[0], mainWindowPosition[1]);
        window.Center();
    }

    public async Task OpenScriptConfigWindowAsync(Script script, string? tab = null)
    {
        const string windowName = "script-config";

        if (_windowManager.FocusExistingWindowIfOpen(windowName))
        {
            return;
        }

        var display = await PrimaryDisplay();

        var queryParams = new List<(string, object?)>();
        queryParams.Add(("script-id", script.Id));
        if (tab != null) queryParams.Add(("tab", tab));

        var window = await _windowManager.CreateWindowAsync(windowName, true, new BrowserWindowOptions
        {
            Title = script.Name,
            Height = display.Bounds.Height * 2 / 3,
            Width = display.Bounds.Width * 4 / 5,
            AutoHideMenuBar = true
        }, queryParams.ToArray());

        window.SetParentWindow(ElectronUtil.MainWindow);
        var mainWindowPosition = await ElectronUtil.MainWindow.GetPositionAsync();
        window.SetPosition(mainWindowPosition[0], mainWindowPosition[1]);
        window.Center();
    }

    public async Task OpenDataConnectionWindowAsync(Guid? dataConnectionId)
    {
        const string windowName = "data-connection";

        if (_windowManager.FocusExistingWindowIfOpen(windowName))
        {
            return;
        }

        var display = await PrimaryDisplay();

        var queryParams = new List<(string, object?)>();
        if (dataConnectionId != null) queryParams.Add(("data-connection-id", dataConnectionId));

        var window = await _windowManager.CreateWindowAsync(windowName, true, new BrowserWindowOptions
        {
            Title = (dataConnectionId.HasValue ? "Edit" : "New") + "Connection",
            Height = display.Bounds.Height * 4 / 10,
            Width = 750,
            AutoHideMenuBar = true,
            MinWidth = 550,
            MinHeight = 550
        }, queryParams.ToArray());

        window.SetParentWindow(ElectronUtil.MainWindow);
        var mainWindowPosition = await ElectronUtil.MainWindow.GetPositionAsync();
        window.SetPosition(mainWindowPosition[0], mainWindowPosition[1]);
        window.Center();
    }

    public async Task OpenOutputWindowAsync()
    {
        const string windowName = "output";

        if (_windowManager.FocusExistingWindowIfOpen(windowName))
        {
            return;
        }

        var display = await PrimaryDisplay();

        var window = await _windowManager.CreateWindowAsync(windowName, true, new BrowserWindowOptions
        {
            Title = "Output",
            Height = display.Bounds.Height * 2 / 3,
            Width = display.Bounds.Width * 4 / 5,
            AutoHideMenuBar = true,
            Show = false
        });

        var mainWindowPosition = await ElectronUtil.MainWindow.GetPositionAsync();
        window.SetPosition(mainWindowPosition[0], mainWindowPosition[1]);
        window.Center();
        window.Maximize();
        window.Show();
    }

    public Task OpenDeveloperToolsAsync(Guid windowId)
    {
        _windowManager.FindWindowAsync(windowId)?.WebContents.OpenDevTools();
        return Task.CompletedTask;
    }

    private void InitializeMenu()
    {
        var menu = new MenuItem[]
        {
            new MenuItem
            {
                Label = "File", Type = MenuType.submenu, Submenu = new MenuItem[]
                {
                    new MenuItem
                    {
                        Label = "New",
                        Accelerator = "CmdOrCtrl+N",
                        Click = async () =>
                        {
                            try
                            {
                                var script = await _mediator.Send(new CreateScriptCommand());
                                await _mediator.Send(new OpenScriptCommand(script));
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error while creating new script");
                            }
                        }
                    },
                    new MenuItem { Type = MenuType.separator },
                    new MenuItem
                    {
                        Label = "Save",
                        Accelerator = "CmdOrCtrl+S",
                        Click = async () =>
                        {
                            try
                            {
                                if (_session.Active != null)
                                {
                                    await _mediator.Send(new SaveScriptCommand(_session.Active.Script));
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error while calling save script");
                            }
                        }
                    },
                    new MenuItem
                    {
                        Label = "Save All",
                        Accelerator = "CmdOrCtrl+Shift+A",
                        Click = async () =>
                        {
                            try
                            {
                                foreach (var environment in _session.Environments.Where(env => env.Script.IsDirty))
                                {
                                    await _mediator.Send(new SaveScriptCommand(environment.Script));
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error while calling save all scripts");
                            }
                        }
                    },
                    new MenuItem
                    {
                        Label = "Properties",
                        Accelerator = "F4",
                        Click = async () =>
                        {
                            try
                            {
                                if (_session.Active != null)
                                {
                                    await OpenScriptConfigWindowAsync(_session.Active.Script);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error while script config window");
                            }
                        }
                    },
                    new MenuItem
                    {
                        Label = "Close",
                        Accelerator = "CmdOrCtrl+W",
                        Click = async () =>
                        {
                            try
                            {
                                if (_session.Active != null)
                                {
                                    await _mediator.Send(new CloseScriptCommand(_session.Active.Script.Id));
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error while calling close script");
                            }
                        }
                    },
                    new MenuItem { Type = MenuType.separator },
                    new MenuItem
                    {
                        Label = "Settings",
                        Accelerator = "F12",
                        Click = async () =>
                        {
                            try
                            {
                                await OpenSettingsWindowAsync();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error opening settings window");
                            }
                        }
                    },
                    new MenuItem { Label = "Exit", Accelerator = "CmdOrCtrl+Q", Role = MenuRole.quit },
                }
            },
            new MenuItem
            {
                Label = "Edit", Type = MenuType.submenu, Submenu = new MenuItem[]
                {
                    new MenuItem { Label = "Undo", Accelerator = "CmdOrCtrl+Z", Role = MenuRole.undo },
                    new MenuItem { Label = "Redo", Accelerator = "Shift+CmdOrCtrl+Z", Role = MenuRole.redo },
                    new MenuItem { Type = MenuType.separator },
                    new MenuItem { Label = "Cut", Accelerator = "CmdOrCtrl+X", Role = MenuRole.cut },
                    new MenuItem { Label = "Copy", Accelerator = "CmdOrCtrl+C", Role = MenuRole.copy },
                    new MenuItem { Label = "Paste", Accelerator = "CmdOrCtrl+V", Role = MenuRole.paste },
                    new MenuItem { Label = "Delete", Role = MenuRole.delete },
                    new MenuItem { Type = MenuType.separator },
                    new MenuItem { Label = "Select All", Accelerator = "CmdOrCtrl+A", Role = MenuRole.selectall }
                }
            },
            new MenuItem
            {
                Label = "View", Type = MenuType.submenu, Submenu = new MenuItem[]
                {
                    new MenuItem { Label = "Reload", Accelerator = "CmdOrCtrl+R", Role = MenuRole.reload },
                    new MenuItem
                    {
                        Label = "Force Reload",
                        Accelerator = "CmdOrCtrl+Shift+R",
                        Click = () =>
                        {
                            try
                            {
                                // On force reload, start fresh and close any old
                                // open secondary windows
                                var mainWindowId = ElectronNET.API.Electron.WindowManager.BrowserWindows.ToList()
                                    .First().Id;
                                ElectronNET.API.Electron.WindowManager.BrowserWindows.ToList().ForEach(browserWindow =>
                                {
                                    if (browserWindow.Id != mainWindowId)
                                    {
                                        browserWindow.Close();
                                    }
                                    else
                                    {
                                        browserWindow.Reload();
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error while force reloading");
                            }
                        }
                    },
                    new MenuItem
                    {
                        Label = "Open Developer Tools",
                        Accelerator = "CmdOrCtrl+Shift+I",
                        Role = MenuRole.toggledevtools
                    },
                    new MenuItem { Type = MenuType.separator },
                    new MenuItem { Label = "Actual Size", Accelerator = "CmdOrCtrl+0", Role = MenuRole.resetzoom },
                    new MenuItem { Label = "Zoom in", Accelerator = "CmdOrCtrl+Plus", Role = MenuRole.zoomin },
                    new MenuItem { Label = "Zoom out", Accelerator = "CmdOrCtrl+-", Role = MenuRole.zoomout },
                    new MenuItem { Type = MenuType.separator },
                    new MenuItem
                    {
                        Label = "Toggle Full Screen",
                        Accelerator = "F11",
                        Role = MenuRole.togglefullscreen
                    }
                }
            },
            new MenuItem
            {
                Label = "Window", Role = MenuRole.window, Type = MenuType.submenu, Submenu = new MenuItem[]
                {
                    new MenuItem { Label = "Minimize", Accelerator = "CmdOrCtrl+M", Role = MenuRole.minimize },
                    new MenuItem { Label = "Zoom", Role = MenuRole.zoom },
                    new MenuItem { Label = "Close", Accelerator = "CmdOrCtrl+W", Role = MenuRole.close }
                }
            },
            new MenuItem
            {
                Label = "Help", Role = MenuRole.help, Type = MenuType.submenu, Submenu = new MenuItem[]
                {
                    new MenuItem
                    {
                        Label = "About",
                        Click = async () =>
                        {
                            try
                            {
                                await OpenSettingsWindowAsync("about");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error opening about window");
                            }
                        }
                    },
                    new MenuItem
                    {
                        Label = "GitHub",
                        Click = async () =>
                            await ElectronNET.API.Electron.Shell.OpenExternalAsync(
                                "https://github.com/tareqimbasher/NetPad")
                    },
                    new MenuItem
                    {
                        Label = "Search Issues",
                        Click = async () =>
                            await ElectronNET.API.Electron.Shell.OpenExternalAsync(
                                "https://github.com/tareqimbasher/NetPad/issues")
                    }
                }
            }
        };

        ElectronNET.API.Electron.Menu.SetApplicationMenu(menu);
    }
}
