using System.Collections.Concurrent;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.Extensions.Logging;

namespace NetPad.Electron.UiInterop;

class BrowserWindowInfo
{
    public BrowserWindowInfo(
        string id,
        string windowName,
        BrowserWindow window,
        bool singleInstance
        )
    {
        Id = id;
        WindowName = windowName;
        Window = window;
        SingleInstance = singleInstance;
    }

    public string Id { get; }
    public string WindowName { get; }
    public BrowserWindow Window { get; }
    public bool SingleInstance { get; }
}

public class WindowManager
{
    private readonly ILogger<WindowManager> _logger;
    private readonly ConcurrentDictionary<string, BrowserWindowInfo> _windows;

    private readonly HostInfo _hostInfo;

    public WindowManager(ILogger<WindowManager> logger, HostInfo hostInfo)
    {
        _logger = logger;
        _hostInfo = hostInfo;
        _windows = new();
    }

    public BrowserWindow? FindWindowAsync(string id)
    {
        return _windows.TryGetValue(id, out var info) ? info.Window : null;
    }

    public async Task<BrowserWindow> CreateWindowAsync(
        string windowName,
        bool singleInstance,
        BrowserWindowOptions options,
        params (string key, object? value)[] queryParams)
    {
        var existing = _windows.Values.FirstOrDefault(w => w.WindowName == windowName);
        if (existing is { SingleInstance: true })
        {
            FocusExistingWindowIfOpen(windowName);
            return existing.Window;
        }

        var windowId = Guid.NewGuid().ToString();
        var url = $"{_hostInfo.HostUrl}?win={windowName}"; //&winId={windowId}

        if (queryParams.Any())
        {
            url += "&" + string.Join("&", queryParams.Select(p => $"{p.key}={p.value}"));
        }

        if (options.MinHeight == 0) options.MinHeight = 400;
        if (options.MinWidth == 0) options.MinWidth = 400;

        options.Center = true;

        var window = await ElectronNET.API.Electron.WindowManager.CreateWindowAsync(options, url);
        _windows.TryAdd(windowId, new BrowserWindowInfo(windowId, windowName, window, singleInstance));

        _logger.LogDebug("Created window with name: {WindowName} and ID: {ID}", windowName, window.Id);

        window.OnClosed += () =>_windows.TryRemove(windowId, out _);

        await window.WebContents.Session.ClearCacheAsync();

        return window;
    }

    public bool FocusExistingWindowIfOpen(string windowName)
    {
        var existing = _windows.Values.FirstOrDefault(w => w.WindowName == windowName);

        if (existing == null) return false;

        existing.Window.Focus();
        return true;
    }
}
