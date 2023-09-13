export class IpcEventNames {
    public static readonly getWindowState = "get-window-state";
    public static readonly maximize = "maximize";
    public static readonly minimize = "minimize";
    public static readonly toggleFullScreen = "toggle-full-screen";
    public static readonly toggleAlwaysOnTop = "toggle-always-on-top";
    public static readonly toggleDeveloperTools = "toggle-developer-tools";
}

export enum WindowViewStatus {
    "Unknown" = "Unknown",
    "Minimized" = "Minimized",
    "UnMaximized" = "UnMaximized",
    "Maximized" = "Maximized",
}

export class WindowState {
    constructor(public viewStatus: WindowViewStatus, public isAlwaysOnTop: boolean) {
    }
}
