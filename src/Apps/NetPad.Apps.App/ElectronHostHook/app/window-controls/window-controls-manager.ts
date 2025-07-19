import {BrowserWindow, ipcMain} from "electron";
import {IWindowState, WindowViewStatus} from "./models";
import {electronConstants} from "../../electron-shared";

export class WindowControlsManager {
    public static init() {
        const handlers = new Map<string, (window: Electron.BrowserWindow) => void | unknown>([
            [electronConstants.ipcEventNames.getWindowState, window => {
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
            [electronConstants.ipcEventNames.maximize, window => window.isMaximized() ? window.unmaximize() : window.maximize()],
            [electronConstants.ipcEventNames.minimize, window => window.isMinimized() ? window.restore() : window.minimize()],
            [electronConstants.ipcEventNames.zoomIn, window => {
                const currentZoomFactor = window.webContents.getZoomFactor();
                window.webContents.setZoomFactor(currentZoomFactor + 0.1);
            }],
            [electronConstants.ipcEventNames.zoomOut, window => {
                const currentZoomFactor = window.webContents.getZoomFactor();
                window.webContents.setZoomFactor(currentZoomFactor - 0.1);
            }],
            [electronConstants.ipcEventNames.resetZoom, window => window.webContents.setZoomFactor(1)],
            [electronConstants.ipcEventNames.toggleFullScreen, window => window.setFullScreen(!window.isFullScreen())],
            [electronConstants.ipcEventNames.toggleAlwaysOnTop, window => window.setAlwaysOnTop(!window.isAlwaysOnTop(), "normal")],
            [electronConstants.ipcEventNames.toggleDeveloperTools, window => window.webContents.toggleDevTools()],
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
