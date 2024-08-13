export enum WindowViewStatus {
    "Unknown" = "Unknown",
    "Minimized" = "Minimized",
    "UnMaximized" = "UnMaximized",
    "Maximized" = "Maximized",
}

export interface IWindowState {
    viewStatus: WindowViewStatus;
    isAlwaysOnTop: boolean;
}
