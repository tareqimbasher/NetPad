import {Socket} from "socket.io";
import {Connector} from "./connector";
import {IpcEventNames, WindowState, WindowViewStatus} from "./models";
import {BrowserWindow, ipcMain} from "electron";

export class HookService extends Connector {
    constructor(socket: Socket, app: Electron.App) {
        super(socket, app);
    }

    public onHostReady(): void {
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

    private getBrowserWindow(event: Electron.IpcMainInvokeEvent): Electron.BrowserWindow | undefined {
        const windowId = event.sender.id;
        return BrowserWindow.getAllWindows().find(w => w.id === windowId);
    }
}
