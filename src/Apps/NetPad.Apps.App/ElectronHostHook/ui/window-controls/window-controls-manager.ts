import {BrowserWindow, ipcMain} from "electron";
import {WindowState, WindowViewStatus} from "./models";

export class IpcEventNames {
    public static readonly getWindowState = "get-window-state";
    public static readonly maximize = "maximize";
    public static readonly minimize = "minimize";
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

                return new WindowState(viewStatus, window.isAlwaysOnTop());
            }],
            [IpcEventNames.maximize, window => window.isMaximized() ? window.unmaximize() : window.maximize()],
            [IpcEventNames.minimize, window => window.isMinimized() ? window.restore() : window.minimize()],
            [IpcEventNames.toggleFullScreen, window => window.setFullScreen(!window.isFullScreen())],
            [IpcEventNames.toggleAlwaysOnTop, window => window.setAlwaysOnTop(!window.isAlwaysOnTop(), "normal")],
            [IpcEventNames.toggleDeveloperTools, window => window.webContents.toggleDevTools()],
        ]);

        for (const [eventName, handler] of handlers) {
            ipcMain.handle(eventName, event => {
                const window = this.getBrowserWindow(event);
                if (!window) return;

                return handler(window);
            });
        }
    }

    private static getBrowserWindow(event: Electron.IpcMainInvokeEvent): Electron.BrowserWindow | undefined {
        const windowId = event.sender.id;
        return BrowserWindow.getAllWindows().find(w => w.id === windowId);
    }
}
