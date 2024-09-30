using System.Collections.Concurrent;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.Extensions.Logging;

namespace NetPad.Apps.Shells.Electron.UiInterop;

public class WindowManager(ILogger<WindowManager> logger, HostInfo hostInfo)
{
    private readonly ConcurrentDictionary<Guid, BrowserWindowInfo> _windows = new();

    public BrowserWindow? FindWindowAsync(Guid id)
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

        var url = $"{hostInfo.HostUrl}?win={windowName}&shell=electron";

        if (queryParams.Any())
        {
            url += "&" + string.Join("&", queryParams.Select(p => $"{p.key}={p.value}"));
        }

        if (options.MinHeight == 0) options.MinHeight = 400;
        if (options.MinWidth == 0) options.MinWidth = 400;

        options.Center = true;

        var window = await ElectronNET.API.Electron.WindowManager.CreateWindowAsync(options, url);

        // We want to add the window ID after creating the window as a hack
        // This hack is needed for when developing the app in watch mode. When making a change to .NET code
        // ElectronNET will restart the app, but that will cause a new main window to be created while keeping the
        // old/existing main window (before the app restart) opened resulting in 2 main windows. ElectronNET
        // determines this new window is different from the old main window because the URL is different.
        // Adding the window ID after creating the window in ElectronNET will stop this from happening.
        var windowId = Guid.NewGuid();
        url += $"&winId={windowId}";
        window.LoadURL(url);

        _windows.TryAdd(windowId, new BrowserWindowInfo(windowId, windowName, window, singleInstance));

        logger.LogDebug("Created window with name: {WindowName} and ID: {ID} and URL: {Url}", windowName, window.Id, url);

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
