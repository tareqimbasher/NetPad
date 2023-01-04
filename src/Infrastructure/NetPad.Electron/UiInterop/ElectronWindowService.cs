using System.Collections.Concurrent;
using ElectronNET.API;
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
    private static readonly ConcurrentDictionary<string, BrowserWindow> _singleInstanceWindows = new();
    private static bool _isMenuInitialized = false;
    private readonly HostInfo _hostInfo;
    private readonly ISession _session;
    private readonly IMediator _mediator;
    private readonly ILogger<ElectronWindowService> _logger;

    public ElectronWindowService(
        HostInfo hostInfo,
        ISession session,
        IMediator mediator,
        ILogger<ElectronWindowService> logger)
    {
        _hostInfo = hostInfo;
        _session = session;
        _mediator = mediator;
        _logger = logger;
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
            AutoHideMenuBar = false
        });

        window.Center();
        window.Maximize();

        lock (_singleInstanceWindows)
        {
            if (!_isMenuInitialized)
            {
                InitializeMenu();
                _isMenuInitialized = true;
            }
        }
    }

    public async Task OpenSettingsWindowAsync(string? tab = null)
    {
        const string windowName = "settings";

        if (FocusExistingWindowIfOpen(windowName))
        {
            return;
        }

        var display = await PrimaryDisplay();

        var queryParams = new List<(string, object?)>();
        if (tab != null) queryParams.Add(("tab", tab));

        var window = await CreateWindowAsync(windowName, true, new BrowserWindowOptions
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

        if (FocusExistingWindowIfOpen(windowName))
        {
            return;
        }

        var display = await PrimaryDisplay();

        var queryParams = new List<(string, object?)>();
        queryParams.Add(("script-id", script.Id));
        if (tab != null) queryParams.Add(("tab", tab));

        var window = await CreateWindowAsync(windowName, true, new BrowserWindowOptions
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

        if (FocusExistingWindowIfOpen(windowName))
        {
            return;
        }

        var display = await PrimaryDisplay();

        var queryParams = new List<(string, object?)>();
        if (dataConnectionId != null) queryParams.Add(("data-connection-id", dataConnectionId));

        var window = await CreateWindowAsync(windowName, true, new BrowserWindowOptions
        {
            Title = (dataConnectionId.HasValue ? "Edit" : "New") + "Connection",
            Height = display.Bounds.Height * 4 / 10,
            Width = 550,
            AutoHideMenuBar = true,
            MinWidth = 550,
            MinHeight = 550
        }, queryParams.ToArray());

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
            _singleInstanceWindows.TryAdd(windowName, window);
            window.OnClosed += () => _singleInstanceWindows.TryRemove(windowName, out _);
        }

        await window.WebContents.Session.ClearCacheAsync();

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
                    new MenuItem { Label = "Quit", Accelerator = "CmdOrCtrl+Q", Role = MenuRole.quit },
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
                                var mainWindowId = ElectronNET.API.Electron.WindowManager.BrowserWindows.ToList().First().Id;
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
                    new MenuItem { Label = "Open Developer Tools", Accelerator = "CmdOrCtrl+Shift+I", Role = MenuRole.toggledevtools },
                    new MenuItem { Type = MenuType.separator },
                    new MenuItem { Label = "Actual Size", Accelerator = "CmdOrCtrl+0", Role = MenuRole.resetzoom },
                    new MenuItem { Label = "Zoom in", Accelerator = "CmdOrCtrl+Plus", Role = MenuRole.zoomin },
                    new MenuItem { Label = "Zoom out", Accelerator = "CmdOrCtrl+-", Role = MenuRole.zoomout },
                    new MenuItem { Type = MenuType.separator },
                    new MenuItem
                    {
                        Label = "Toggle Full Screen",
                        Accelerator = "F11",
                        Click = async () =>
                        {
                            try
                            {
                                bool isFullScreen = await ElectronNET.API.Electron.WindowManager.BrowserWindows.First().IsFullScreenAsync();
                                ElectronNET.API.Electron.WindowManager.BrowserWindows.First().SetFullScreen(!isFullScreen);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error while toggling full screen");
                            }
                        },
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
                        Click = async () => await ElectronNET.API.Electron.Shell.OpenExternalAsync("https://github.com/tareqimbasher/NetPad")
                    },
                    new MenuItem
                    {
                        Label = "Search Issues",
                        Click = async () => await ElectronNET.API.Electron.Shell.OpenExternalAsync("https://github.com/tareqimbasher/NetPad/issues")
                    }
                }
            }
        };

        ElectronNET.API.Electron.Menu.SetApplicationMenu(menu);
    }
}
