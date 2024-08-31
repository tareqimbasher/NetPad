import {BrowserWindow, ipcMain} from "electron";
import {IWindowState, WindowViewStatus} from "./models";

export class IpcEventNames {
    public static readonly getWindowState = "get-window-state";
    public static readonly maximize = "maximize";
    public static readonly minimize = "minimize";
    public static readonly zoomIn = "zoom-in";
    public static readonly zoomOut = "zoom-out";
    public static readonly resetZoom = "reset-zoom";
    public static readonly toggleFullScreen = "toggle-full-screen";
    public static readonly toggleAlwaysOnTop = "toggle-always-on-top";
    public static readonly toggleDeveloperTools = "toggle-developer-tools";
}

export class WindowControlsManager {
    public static init() {
        const handlers = new Map<string, (window: Electron.BrowserWindow) => void | unknown>([
            [IpcEventNames.getWindowState, window => {
                let viewStatus: WindowViewStatus;

                if (window.isMaximized())
                    viewStatus = WindowViewStatus.Maximized;
                else if (window.isMinimized())
                    viewStatus = WindowViewStatus.Minimized;
                else
                    viewStatus = WindowViewStatus.UnMaximized;

                return <IWindowState>{
                    viewStatus: viewStatus,
                    isAlwaysOnTop: window.isAlwaysOnTop()
                };
            }],
            [IpcEventNames.maximize, window => window.isMaximized() ? window.unmaximize() : window.maximize()],
            [IpcEventNames.minimize, window => window.isMinimized() ? window.restore() : window.minimize()],
            [IpcEventNames.zoomIn, window => {
                const currentZoomFactor = window.webContents.getZoomFactor();
                window.webContents.setZoomFactor(currentZoomFactor + 0.1);
            }],
            [IpcEventNames.zoomOut, window => {
                const currentZoomFactor = window.webContents.getZoomFactor();
                window.webContents.setZoomFactor(currentZoomFactor - 0.1);
            }],
            [IpcEventNames.resetZoom, window => window.webContents.setZoomFactor(1)],
            [IpcEventNames.toggleFullScreen, window => window.setFullScreen(!window.isFullScreen())],
            [IpcEventNames.toggleAlwaysOnTop, window => window.setAlwaysOnTop(!window.isAlwaysOnTop(), "normal")],
            [IpcEventNames.toggleDeveloperTools, window => window.webContents.toggleDevTools()],
        ]);

        for (const [eventName, handler] of handlers) {
            ipcMain.handle(eventName, event => {
                try {
                    const window = this.getBrowserWindow(event);
                    if (!window) {
                        return;
                    }

                    return handler(window);
                } catch (e) {
                    console.error(`Error while handling event: '${eventName}'`, e);
                }
            });
        }
    }

    private static getBrowserWindow(event: Electron.IpcMainInvokeEvent): Electron.BrowserWindow | undefined {
        const windowId = event.sender.id;
        return BrowserWindow.getAllWindows().find(w => w.id === windowId);
    }
}
